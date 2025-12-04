using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SelfOrderingSystemKiosk.Models
{
    public class ChickenFlavors
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("category")]
        public string Category { get; set; }

        [BsonElement("image")]
        public string Image { get; set; }

        [BsonElement("availability")]
        public string Availability { get; set; } = "Available";

        // Inventory fields
        [BsonElement("currentStock")]
        public int CurrentStock { get; set; } = 0;

        [BsonElement("unit")]
        public string Unit { get; set; } = "pcs";

        [BsonElement("reorderLevel")]
        public int ReorderLevel { get; set; } = 10;

        [BsonElement("status")]
        public string Status { get; set; } = "In Stock";
    }
}
