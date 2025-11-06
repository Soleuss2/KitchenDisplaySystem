using System;

namespace SelfOrderingSystemKiosk.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; }
        public string Items { get; set; }
        public double Total { get; set; }
        public DateTime DateTime { get; set; }
        public string Status { get; set; }
    }
}
