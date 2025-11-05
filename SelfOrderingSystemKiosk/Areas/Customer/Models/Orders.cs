using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SelfOrderingSystemKiosk.Areas.Customer.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("orderNumber")]
        public string OrderNumber { get; set; }

        [BsonElement("orderDate")]
        public DateTime OrderDate { get; set; }

        [BsonElement("orderType")]
        public string OrderType { get; set; }

        [BsonElement("items")]
        public List<OrderItem> Items { get; set; }

        [BsonElement("subtotal")]
        public decimal Subtotal { get; set; }

        [BsonElement("tax")]
        public decimal Tax { get; set; }

        [BsonElement("total")]
        public decimal Total { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        public Order()
        {
            Items = new List<OrderItem>();
            OrderDate = DateTime.Now;
            Status = "Pending";
            OrderType = "AlaCarte";
        }
    }

    public class OrderItem
    {
        [BsonElement("itemName")]
        public string ItemName { get; set; }

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }
    }
}