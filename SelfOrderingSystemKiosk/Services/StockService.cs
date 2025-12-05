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

        public StockService(IMongoClient mongoClient, IConfiguration config)
        {
            // Database & collection names from appsettings
            var dbName = config["KitchenDatabase:DatabaseName"];        // "Kitchen"
            var collectionName = config["KitchenDatabase:InventoryItem"]; // "Stock"

            var database = mongoClient.GetDatabase(dbName);
            _stockCollection = database.GetCollection<InventoryItem>(collectionName);
        }



        public async Task<List<InventoryItem>> GetAllAsync() =>
            await _stockCollection.Find(_ => true).ToListAsync();

        public async Task<List<InventoryItem>> GetAvailableAsync()
        {
            // Get all items and filter in memory to handle items without Availability field
            var allItems = await _stockCollection.Find(_ => true).ToListAsync();
            return allItems.Where(x => 
                x.Availability == null || 
                x.Availability == "" || 
                x.Availability == "Available"
            ).ToList();
        }

        public async Task<InventoryItem> GetByIdAsync(string id) =>
            await _stockCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task AddAsync(InventoryItem item)
        {
            // Automatically set availability based on stock for new items
            if (item.CurrentStock == 0)
            {
                item.Availability = "Unavailable";
            }
            else if (string.IsNullOrEmpty(item.Availability))
            {
                item.Availability = "Available";
            }

            await _stockCollection.InsertOneAsync(item);
        }

        public async Task UpdateAsync(InventoryItem item)
        {
            // Automatically set availability based on stock
            if (item.CurrentStock == 0)
            {
                item.Availability = "Unavailable";
            }
            else if (string.IsNullOrEmpty(item.Availability) || item.Availability == "Unavailable")
            {
                // If stock is > 0 and availability is not set or is Unavailable, set to Available
                item.Availability = "Available";
            }

            await _stockCollection.ReplaceOneAsync(x => x.Id == item.Id, item);
        }

        public async Task DeleteAsync(string id) =>
            await _stockCollection.DeleteOneAsync(x => x.Id == id);

        public async Task ToggleAvailabilityAsync(string id, string availability)
        {
            var update = Builders<InventoryItem>.Update.Set(x => x.Availability, availability);
            await _stockCollection.UpdateOneAsync(x => x.Id == id, update);
        }

        // Get inventory item by item name
        public async Task<InventoryItem> GetByNameAsync(string itemName)
        {
            return await _stockCollection.Find(x => x.Item == itemName).FirstOrDefaultAsync();
        }

        // Decrement stock by item name and quantity
        public async Task<bool> DecrementStockAsync(string itemName, int quantity)
        {
            var item = await GetByNameAsync(itemName);
            if (item == null)
            {
                // Item not found in inventory
                Console.WriteLine($"Warning: Item '{itemName}' not found in inventory.");
                return false;
            }

            // Calculate new stock (ensure it doesn't go below 0)
            var newStock = Math.Max(0, item.CurrentStock - quantity);

            // Determine availability based on stock
            var availability = newStock == 0 ? "Unavailable" : "Available";

            // Update stock, status, and availability
            var update = Builders<InventoryItem>.Update
                .Set(x => x.CurrentStock, newStock)
                .Set(x => x.Status, newStock <= item.ReorderLevel ? "Low Stock" : "In Stock")
                .Set(x => x.Availability, availability);

            var result = await _stockCollection.UpdateOneAsync(
                x => x.Item == itemName,
                update
            );

            return result.ModifiedCount > 0;
        }
    }
}
