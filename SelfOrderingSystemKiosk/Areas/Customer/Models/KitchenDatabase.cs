using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace SelfOrderingSystemKiosk.Models
{
    public class KitchenDatabase
    {

        private readonly IMongoDatabase _database;

        public KitchenDatabase(IOptions<MongoDBSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        // Expose it publicly
        public IMongoDatabase Database => _database;


        // 👇 Add this line — it defines your ChickenFlavors collection
        public IMongoCollection<ChickenFlavors> ChickenFlavors =>
            Database.GetCollection<ChickenFlavors>("ChickenWings_Flavor");
   
    }
}
