using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Models;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    public class MenuController : Controller
    {
        private readonly StockService _stockService;

        public MenuController(StockService stockService)
        {
            _stockService = stockService;
        }

        public async Task<IActionResult> Index(string message = null)
        {
            ViewData["Title"] = "Menu & Inventory Management";
            ViewBag.Message = message;
            var items = await _stockService.GetAllAsync();
            return View(items);
        }


        [HttpPost]
        public async Task<IActionResult> Add(string name, string category, decimal price, string availability, int currentStock, string unit, int reorderLevel)
        {
            if (!string.IsNullOrEmpty(name))
            {
                // Set default image based on category
                string defaultImage = category switch
                {
                    "Wings" => "/images/wings.png",
                    "Appetizer" => "/images/appetize.png",
                    "Add Ons" => "/images/wings.png",
                    _ => "/images/wings.png"
                };

                var newItem = new InventoryItem
                {
                    Item = name,
                    Category = category,
                    Price = price,
                    Availability = currentStock == 0 ? "Unavailable" : (availability ?? "Available"),
                    Image = defaultImage,
                    CurrentStock = currentStock,
                    Unit = unit ?? "pcs",
                    ReorderLevel = reorderLevel,
                    Status = currentStock <= reorderLevel ? "Low Stock" : "In Stock"
                };

                await _stockService.AddAsync(newItem);
            }

            return RedirectToAction("Index", new { message = "Menu item added successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _stockService.DeleteAsync(id);
            return RedirectToAction("Index", new { message = "Menu item deleted successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(InventoryItem updated)
        {
            var existing = await _stockService.GetByIdAsync(updated.Id);
            if (existing != null)
            {
                // Preserve the existing image
                updated.Image = existing.Image;
                
                // Set default image based on category if image is empty
                if (string.IsNullOrEmpty(updated.Image))
                {
                    updated.Image = updated.Category switch
                    {
                        "Wings" => "/images/wings.png",
                        "Appetizer" => "/images/appetize.png",
                        "Add Ons" => "/images/wings.png",
                        _ => "/images/wings.png"
                    };
                }
                
                updated.Status = updated.CurrentStock <= updated.ReorderLevel ? "Low Stock" : "In Stock";
                // Availability will be automatically set by UpdateAsync based on stock
                await _stockService.UpdateAsync(updated);
            }

            return RedirectToAction("Index", new { message = "Menu item updated successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(string id, string availability)
        {
            try
            {
                await _stockService.ToggleAvailabilityAsync(id, availability);
                
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                {
                    return Json(new { success = true, message = $"Item availability set to {availability}!" });
                }
                
                return RedirectToAction("Index", new { message = $"Item availability set to {availability}!" });
            }
            catch (Exception ex)
            {
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.Headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded"))
                {
                    return Json(new { success = false, message = "Failed to update availability." });
                }
                
                return RedirectToAction("Index", new { message = "Failed to update availability." });
            }
        }
    }
}
