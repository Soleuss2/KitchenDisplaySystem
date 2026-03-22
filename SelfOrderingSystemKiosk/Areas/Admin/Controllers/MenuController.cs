using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SelfOrderingSystemKiosk.Models;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Kitchen")]
    public class MenuController : Controller
    {
        private readonly StockService _stockService;
        private readonly IWebHostEnvironment _environment;
        private readonly MenuCategoryRegistry _menuCategories;

        public MenuController(StockService stockService, IWebHostEnvironment environment, MenuCategoryRegistry menuCategories)
        {
            _stockService = stockService;
            _environment = environment;
            _menuCategories = menuCategories;
        }

        public async Task<IActionResult> Index(string message = null, string categoryFilter = null)
        {
            ViewData["Title"] = "Menu & Inventory Management";
            ViewBag.Message = message;
            ViewBag.MenuCategories = _menuCategories.All;
            var filter = string.IsNullOrWhiteSpace(categoryFilter) || string.Equals(categoryFilter, "all", StringComparison.OrdinalIgnoreCase)
                ? null
                : categoryFilter.Trim();
            ViewBag.CategoryFilter = filter ?? "all";

            var allItems = await _stockService.GetAllAsync();
            ViewBag.MenuCategoryFormList = BuildEditCategoryOptions(allItems);

            var items = allItems;
            if (filter != null && _menuCategories.IsValidKey(filter))
                items = allItems.Where(i => string.Equals(i.Category, filter, StringComparison.Ordinal)).ToList();

            return View(items);
        }

        private List<MenuCategoryOption> BuildEditCategoryOptions(IEnumerable<InventoryItem> items)
        {
            var list = _menuCategories.All.ToList();
            var keys = new HashSet<string>(list.Select(c => c.Key), StringComparer.Ordinal);
            foreach (var i in items)
            {
                if (string.IsNullOrWhiteSpace(i.Category)) continue;
                if (keys.Add(i.Category))
                    list.Add(new MenuCategoryOption { Key = i.Category, DisplayName = i.Category + " (legacy)" });
            }

            return list.OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }


        [HttpPost]
        public async Task<IActionResult> Add(string name, string category, decimal price, string availability, int currentStock, string unit, int reorderLevel, int menuOrder, IFormFile imageFile)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (!_menuCategories.IsValidKey(category))
                    return RedirectToAction("Index", new { message = "Invalid category selected.", categoryFilter = "all" });

                string imagePath = null;

                if (imageFile != null && imageFile.Length > 0)
                    imagePath = await SaveImageFile(imageFile);

                if (string.IsNullOrEmpty(imagePath))
                    imagePath = _menuCategories.GetDefaultImage(category);

                var effectiveAvailability = string.Equals(category, "Unavailable", StringComparison.Ordinal)
                    ? "Unavailable"
                    : (currentStock == 0 ? "Unavailable" : (availability ?? "Available"));

                var newItem = new InventoryItem
                {
                    Item = name,
                    Category = category,
                    Price = price,
                    Availability = effectiveAvailability,
                    Image = imagePath,
                    CurrentStock = currentStock,
                    Unit = unit ?? "pcs",
                    ReorderLevel = reorderLevel,
                    MenuOrder = menuOrder,
                    Status = currentStock <= reorderLevel ? "Low Stock" : "In Stock"
                };

                await _stockService.AddAsync(newItem);
            }

            return RedirectToAction("Index", new { message = "Menu item added successfully!" });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(InventoryItem updated, IFormFile imageFile)
        {
            var existing = await _stockService.GetByIdAsync(updated.Id);
            if (existing != null)
            {
                var categoryOk = _menuCategories.IsValidKey(updated.Category)
                    || string.Equals(updated.Category, existing.Category, StringComparison.Ordinal);
                if (!categoryOk)
                    return RedirectToAction("Index", new { message = "Invalid category selected." });

                if (imageFile != null && imageFile.Length > 0)
                {
                    var newImagePath = await SaveImageFile(imageFile);
                    if (!string.IsNullOrEmpty(newImagePath))
                        updated.Image = newImagePath;
                }
                else
                    updated.Image = existing.Image ?? updated.Image;

                if (string.IsNullOrEmpty(updated.Image))
                    updated.Image = _menuCategories.GetDefaultImage(updated.Category);

                if (string.Equals(updated.Category, "Unavailable", StringComparison.Ordinal))
                    updated.Availability = "Unavailable";

                updated.Status = updated.CurrentStock <= updated.ReorderLevel ? "Low Stock" : "In Stock";
                await _stockService.UpdateAsync(updated);
            }

            return RedirectToAction("Index", new { message = "Menu item updated successfully!" });
        }
        
        private async Task<string> SaveImageFile(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return null;
            }

            // Create items directory if it doesn't exist
            var itemsDirectory = Path.Combine(_environment.WebRootPath, "images", "items");
            if (!Directory.Exists(itemsDirectory))
            {
                Directory.CreateDirectory(itemsDirectory);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(itemsDirectory, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Return relative path for database storage
            return $"/images/items/{fileName}";
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
