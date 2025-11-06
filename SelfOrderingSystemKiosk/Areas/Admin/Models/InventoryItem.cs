namespace SelfOrderingSystemKiosk.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public string Item { get; set; }
        public string Category { get; set; }
        public int CurrentStock { get; set; }
        public string Unit { get; set; }
        public int ReorderLevel { get; set; }
        public string Status { get; set; }
    }
}
