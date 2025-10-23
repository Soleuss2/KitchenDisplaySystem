using MongoDB.Driver;
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

        public async Task<ChickenFlavors> GetByIdAsync(string id) =>
            await _wingCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
}
