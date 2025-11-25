using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    public class MenuController : Controller
    {
        // temp data
        private static List<MenuItem> menuItems = new List<MenuItem>
        {
            new MenuItem { Id = 1, Name = "Classic Burger", Category = "Main Dish", Price = 120.00m, Availability = "Available" },
            new MenuItem { Id = 2, Name = "Cheese Fries", Category = "Side", Price = 80.00m, Availability = "Available" },
            new MenuItem { Id = 3, Name = "Iced Coffee", Category = "Beverage", Price = 60.00m, Availability = "Unavailable" }
        };

        public IActionResult Index(string message = null)
        {
            ViewBag.Message = message;
            return View(menuItems);
        }

        [HttpPost]
        public IActionResult Add(string name, string category, decimal price, string availability)
        {
            if (!string.IsNullOrEmpty(name))
            {
                int newId = menuItems.Any() ? menuItems.Max(m => m.Id) + 1 : 1;
                menuItems.Add(new MenuItem
                {
                    Id = newId,
                    Name = name,
                    Category = category,
                    Price = price,
                    Availability = availability
                });
            }

            return RedirectToAction("Index", new { message = "Menu item added successfully!" });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var item = menuItems.FirstOrDefault(m => m.Id == id);
            if (item != null)
                menuItems.Remove(item);

            return RedirectToAction("Index", new { message = "Menu item deleted successfully!" });
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = menuItems.FirstOrDefault(m => m.Id == id);
            if (item == null)
                return RedirectToAction("Index");

            return View(item);
        }

        [HttpPost]
        public IActionResult Edit(MenuItem updated)
        {
            var existing = menuItems.FirstOrDefault(m => m.Id == updated.Id);
            if (existing != null)
            {
                existing.Name = updated.Name;
                existing.Category = updated.Category;
                existing.Price = updated.Price;
                existing.Availability = updated.Availability;
            }

            return RedirectToAction("Index", new { message = "Menu item updated successfully!" });
        }
    }

    public class MenuItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public string? Availability { get; set; }
    }
}
