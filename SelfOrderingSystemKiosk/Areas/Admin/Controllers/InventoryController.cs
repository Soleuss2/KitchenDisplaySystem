using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    public class InventoryController : Controller
    {
        // Temporary in-memory data (replace with DB later)
        private static List<InventoryItem> inventory = new List<InventoryItem>
        {
            new InventoryItem { Id = 1, Item = "Burger Patty", Category = "Food", CurrentStock = 30, Unit = "pcs", ReorderLevel = 10, Status = "In Stock" },
            new InventoryItem { Id = 2, Item = "Fries", Category = "Food", CurrentStock = 8, Unit = "kg", ReorderLevel = 5, Status = "Low Stock" },
            new InventoryItem { Id = 3, Item = "Paper Cups", Category = "Supplies", CurrentStock = 50, Unit = "pcs", ReorderLevel = 20, Status = "In Stock" }
        };

        public IActionResult Index()
        {
            TempData["Message"] = TempData["Message"];
            return View(inventory);
        }

        [HttpPost]
        public IActionResult Add(string item, string category, int stock, string unit, int reorderLevel)
        {
            if (!string.IsNullOrEmpty(item) && !string.IsNullOrEmpty(category))
            {
                int newId = inventory.Any() ? inventory.Max(i => i.Id) + 1 : 1;
                string status = stock <= reorderLevel ? "Low Stock" : "In Stock";

                inventory.Add(new InventoryItem
                {
                    Id = newId,
                    Item = item,
                    Category = category,
                    CurrentStock = stock,
                    Unit = unit,
                    ReorderLevel = reorderLevel,
                    Status = status
                });

                TempData["Message"] = "Item successfully added!";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = inventory.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                inventory.Remove(item);
                TempData["Message"] = $"Item '{item.Item}' deleted successfully!";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = inventory.FirstOrDefault(i => i.Id == id);
            if (item == null)
                return RedirectToAction("Index");

            return View(item);
        }

        [HttpPost]
        public IActionResult Edit(InventoryItem updatedItem)
        {
            var existing = inventory.FirstOrDefault(i => i.Id == updatedItem.Id);
            if (existing != null)
            {
                existing.Item = updatedItem.Item;
                existing.Category = updatedItem.Category;
                existing.CurrentStock = updatedItem.CurrentStock;
                existing.Unit = updatedItem.Unit;
                existing.ReorderLevel = updatedItem.ReorderLevel;
                existing.Status = updatedItem.CurrentStock <= updatedItem.ReorderLevel ? "Low Stock" : "In Stock";

                TempData["Message"] = $"Item '{updatedItem.Item}' updated successfully!";
            }

            return RedirectToAction("Index");
        }
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public string? Item { get; set; }
        public string? Category { get; set; }
        public int CurrentStock { get; set; }
        public string? Unit { get; set; }
        public int ReorderLevel { get; set; }
        public string? Status { get; set; }
    }
}
