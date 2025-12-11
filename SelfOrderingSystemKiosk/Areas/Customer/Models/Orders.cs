using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace SelfOrderingSystemKiosk.Areas.Customer.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("orderNumber")]
        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; }

        [BsonElement("orderDate")]
        [JsonPropertyName("orderDate")]
        public DateTime OrderDate { get; set; }

        [BsonElement("orderType")]
        [JsonPropertyName("orderType")]
        public string OrderType { get; set; }

        [BsonElement("diningType")]
        [JsonPropertyName("diningType")]
        public string DiningType { get; set; }

        [BsonElement("items")]
        [JsonPropertyName("items")]
        public List<OrderItem> Items { get; set; }

        [BsonElement("subtotal")]
        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [BsonElement("tax")]
        [JsonPropertyName("tax")]
        public decimal Tax { get; set; }

        [BsonElement("total")]
        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [BsonElement("status")]
        [JsonPropertyName("status")]
        public string Status { get; set; }

        public Order()
        {
            Items = new List<OrderItem>();
            OrderDate = DateTime.UtcNow;
            Status = "Pending";
            OrderType = "AlaCarte";
        }
    }

    public class OrderItem
    {
        [BsonElement("itemName")]
        [JsonPropertyName("ItemName")]
        public string ItemName { get; set; }

        [BsonElement("quantity")]
        [JsonPropertyName("Quantity")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        [JsonPropertyName("Price")]
        public decimal Price { get; set; }
    }
}