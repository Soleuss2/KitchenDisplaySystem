namespace SelfOrderingSystemKiosk.Models
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string OrdersCollectionName { get; set; } = string.Empty;
    }
}