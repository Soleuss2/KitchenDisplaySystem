using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SelfOrderingSystemKiosk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SelfOrderingSystemKiosk.Services
{
    public class StockService
    {
        private readonly IMongoCollection<InventoryItem> _stockCollection;
        private readonly StockMovementService _movements;
        private readonly ILogger<StockService> _logger;

        public StockService(IMongoClient mongoClient, IConfiguration config, StockMovementService movements, ILogger<StockService> logger)
        {
            var dbName = config["KitchenDatabase:DatabaseName"];        // "Kitchen"
            var collectionName = config["KitchenDatabase:InventoryItem"]; // "Stock"

            var database = mongoClient.GetDatabase(dbName);
            _stockCollection = database.GetCollection<InventoryItem>(collectionName);
            _movements = movements;
            _logger = logger;
        }

        /// <summary>Matches kiosk “available” items (null/empty/Available).</summary>
        private static bool IsAvailableForCustomerMenu(string? availability)
        {
            if (string.IsNullOrEmpty(availability)) return true;
            return string.Equals(availability, "Available", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<InventoryItem>> GetAllAsync()
        {
            var list = await _stockCollection.Find(_ => true).ToListAsync();
            return list
                .OrderBy(i => IsAvailableForCustomerMenu(i.Availability) ? 0 : 1)
                .ThenByDescending(i => i.MenuOrder)
                .ThenBy(i => i.Item ?? "", StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<List<InventoryItem>> GetAvailableAsync()
        {
            var availableOrUnset = Builders<InventoryItem>.Filter.Or(
                Builders<InventoryItem>.Filter.Eq(x => x.Availability, (string)null),
                Builders<InventoryItem>.Filter.Eq(x => x.Availability, ""),
                Builders<InventoryItem>.Filter.Eq(x => x.Availability, "Available"),
                Builders<InventoryItem>.Filter.Not(
                    Builders<InventoryItem>.Filter.Exists(x => x.Availability)));

            var list = await _stockCollection.Find(availableOrUnset).ToListAsync();
            return list
                .OrderByDescending(i => i.MenuOrder)
                .ThenBy(i => i.Item ?? "", StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<InventoryItem> GetByIdAsync(string id) =>
            await _stockCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task AddAsync(InventoryItem item)
        {
            if (string.Equals(item.Category, "Unavailable", StringComparison.Ordinal))
                item.Availability = "Unavailable";
            else if (item.CurrentStock == 0)
                item.Availability = "Unavailable";
            else if (string.IsNullOrEmpty(item.Availability))
                item.Availability = "Available";

            await _stockCollection.InsertOneAsync(item);

            if (item.CurrentStock > 0)
            {
                await _movements.InsertAsync(new StockMovement
                {
                    InventoryItemId = item.Id,
                    ItemName = item.Item ?? "",
                    QuantityDelta = item.CurrentStock,
                    StockBefore = 0,
                    StockAfter = item.CurrentStock,
                    Reason = "Initial",
                    ReferenceType = "Inventory",
                    ReferenceId = item.Id,
                    Note = "New item added"
                });
            }
        }

        public async Task UpdateAsync(InventoryItem item)
        {
            if (string.Equals(item.Category, "Unavailable", StringComparison.Ordinal))
                item.Availability = "Unavailable";
            else if (item.CurrentStock == 0)
                item.Availability = "Unavailable";
            else if (string.IsNullOrEmpty(item.Availability))
                item.Availability = "Available";

            await _stockCollection.ReplaceOneAsync(x => x.Id == item.Id, item);
        }

        public async Task DeleteAsync(string id) =>
            await _stockCollection.DeleteOneAsync(x => x.Id == id);

        public async Task ToggleAvailabilityAsync(string id, string availability)
        {
            var update = Builders<InventoryItem>.Update.Set(x => x.Availability, availability);
            await _stockCollection.UpdateOneAsync(x => x.Id == id, update);
        }

        public async Task<InventoryItem> GetByNameAsync(string itemName)
        {
            return await _stockCollection.Find(x => x.Item == itemName).FirstOrDefaultAsync();
        }

        /// <summary>Decrements stock when an order is completed. Records a Sale movement when successful.</summary>
        public async Task<bool> DecrementStockAsync(string itemName, int quantity, string? reason = null, string? referenceType = null, string? referenceId = null)
        {
            var item = await GetByNameAsync(itemName);
            if (item == null)
            {
                _logger.LogWarning("DecrementStock: item '{Item}' not found in inventory.", itemName);
                return false;
            }

            var oldStock = item.CurrentStock;
            var newStock = Math.Max(0, oldStock - quantity);
            var availability = newStock == 0 ? "Unavailable" : "Available";

            var update = Builders<InventoryItem>.Update
                .Set(x => x.CurrentStock, newStock)
                .Set(x => x.Status, newStock <= item.ReorderLevel ? "Low Stock" : "In Stock")
                .Set(x => x.Availability, availability);

            var result = await _stockCollection.UpdateOneAsync(
                x => x.Item == itemName,
                update
            );

            if (result.ModifiedCount == 0)
                return false;

            var delta = newStock - oldStock;
            if (delta != 0)
            {
                await _movements.InsertAsync(new StockMovement
                {
                    InventoryItemId = item.Id,
                    ItemName = item.Item ?? itemName,
                    QuantityDelta = delta,
                    StockBefore = oldStock,
                    StockAfter = newStock,
                    Reason = reason ?? "Sale",
                    ReferenceType = referenceType ?? "Order",
                    ReferenceId = referenceId,
                    Note = null
                });
            }

            return true;
        }

        /// <summary>Increases stock (e.g. dashboard restock). Records a Restock movement.</summary>
        public async Task<bool> IncreaseStockAsync(string inventoryItemId, int quantityAdded, string referenceType, string? referenceId, string? note = null)
        {
            if (quantityAdded <= 0)
                return false;

            var item = await GetByIdAsync(inventoryItemId);
            if (item == null)
                return false;

            var oldStock = item.CurrentStock;
            item.CurrentStock += quantityAdded;
            item.Status = item.CurrentStock <= item.ReorderLevel ? "Low Stock" : "In Stock";
            item.Availability = item.CurrentStock == 0 ? "Unavailable" : "Available";

            await UpdateAsync(item);

            await _movements.InsertAsync(new StockMovement
            {
                InventoryItemId = item.Id,
                ItemName = item.Item ?? "",
                QuantityDelta = quantityAdded,
                StockBefore = oldStock,
                StockAfter = item.CurrentStock,
                Reason = "Restock",
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                Note = note
            });

            return true;
        }

        /// <summary>Logs a manual stock correction without changing inventory (caller updates item separately) or use after UpdateAsync from edit.</summary>
        public async Task RecordAdjustmentAsync(string inventoryItemId, string itemName, int stockBefore, int stockAfter, string note)
        {
            await _movements.InsertAsync(new StockMovement
            {
                InventoryItemId = inventoryItemId,
                ItemName = itemName,
                QuantityDelta = stockAfter - stockBefore,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Reason = "Adjustment",
                ReferenceType = "Inventory",
                ReferenceId = inventoryItemId,
                Note = note
            });
        }
    }
}
