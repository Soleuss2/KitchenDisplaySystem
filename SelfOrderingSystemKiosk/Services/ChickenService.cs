using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SelfOrderingSystemKiosk.Models;


namespace SelfOrderingSystemKiosk.Services
{
    public class ChickenService
    {

        private readonly IMongoCollection<ChickenFlavors> _wingCollection;

        public ChickenService(KitchenDatabase mongoDBService)
        {
            _wingCollection = mongoDBService.ChickenFlavors;
        }

        public async Task<List<ChickenFlavors>> GetAllAsync() =>
            await _wingCollection.Find(_ => true).ToListAsync();

        public async Task<List<ChickenFlavors>> GetAvailableAsync() =>
            await _wingCollection.Find(x => x.Availability == "Available").ToListAsync();

        public async Task<ChickenFlavors> GetByIdAsync(string id) =>
            await _wingCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(ChickenFlavors item) =>
            await _wingCollection.InsertOneAsync(item);

        public async Task UpdateAsync(ChickenFlavors item) =>
            await _wingCollection.ReplaceOneAsync(x => x.Id == item.Id, item);

        public async Task DeleteAsync(string id) =>
            await _wingCollection.DeleteOneAsync(x => x.Id == id);

        public async Task ToggleAvailabilityAsync(string id, string availability)
        {
            var update = Builders<ChickenFlavors>.Update.Set(x => x.Availability, availability);
            await _wingCollection.UpdateOneAsync(x => x.Id == id, update);
        }
    }
}
