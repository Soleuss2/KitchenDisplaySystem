using MongoDB.Driver;
using SelfOrderingSystemKiosk.Models;
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

        public async Task AddAsync(InventoryItem item) =>
            await _stockCollection.InsertOneAsync(item);

        public async Task UpdateAsync(InventoryItem item) =>
            await _stockCollection.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _stockCollection.DeleteOneAsync(x => x.Id == id);

        public async Task ToggleAvailabilityAsync(string id, string availability)
        {
            var update = Builders<InventoryItem>.Update.Set(x => x.Availability, availability);
            await _stockCollection.UpdateOneAsync(x => x.Id == id, update);
        }
    }
}
