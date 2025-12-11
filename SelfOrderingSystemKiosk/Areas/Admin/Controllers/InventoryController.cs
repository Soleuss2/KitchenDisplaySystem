using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SelfOrderingSystemKiosk.Models;
using SelfOrderingSystemKiosk.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Kitchen")]
    public class InventoryController : Controller
    {
        private readonly StockService _stockService;

        public InventoryController(StockService stockService)
        {
            _stockService = stockService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Inventory Management";
            var items = await _stockService.GetAllAsync();
            ViewBag.ItemCount = items?.Count ?? 0;
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> SyncFromMenu()
        {
            try
            {
                // This will be called from MenuController's sync method
                // For now, just return success
                return Json(new { success = true, message = "Use Menu Management to sync items to Stock." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Add(string item, string category, int stock, string unit, int reorderLevel, decimal price)
        {
            var newItem = new InventoryItem
            {
                Item = item,
                Category = category,
                CurrentStock = stock,
                Unit = unit,
                ReorderLevel = reorderLevel,
                Price = price,
                Status = stock <= reorderLevel ? "Low Stock" : "In Stock",
                Availability = stock == 0 ? "Unavailable" : "Available"
            };

            await _stockService.AddAsync(newItem);
            TempData["Message"] = "Item successfully added!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _stockService.DeleteAsync(id);
            TempData["Message"] = $"Item deleted successfully!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string id)
        {
            var item = await _stockService.GetByIdAsync(id);
            if (item == null) return RedirectToAction("Index");
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(InventoryItem updatedItem)
        {
            updatedItem.Status = updatedItem.CurrentStock <= updatedItem.ReorderLevel ? "Low Stock" : "In Stock";
            // Availability will be automatically set by UpdateAsync based on stock
            await _stockService.UpdateAsync(updatedItem);
            TempData["Message"] = $"Item '{updatedItem.Item}' updated successfully!";
            return RedirectToAction("Index");
        }
    }
}
