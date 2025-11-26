using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SelfOrderingSystemKiosk.Models
{
    public class InventoryItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }  // MongoDB ObjectId

        [BsonElement("Item")]
        public string Item { get; set; }

        [BsonElement("Category")]
        public string Category { get; set; }

        [BsonElement("CurrentStock")]
        public int CurrentStock { get; set; }

        [BsonElement("Unit")]
        public string Unit { get; set; }

        [BsonElement("ReorderLevel")]
        public int ReorderLevel { get; set; }

        [BsonElement("Price")]
        public decimal Price { get; set; }

        [BsonElement("Status")]
        public string Status { get; set; }
    }
}
