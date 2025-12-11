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

        public MenuController(StockService stockService, IWebHostEnvironment environment)
        {
            _stockService = stockService;
            _environment = environment;
        }

        public async Task<IActionResult> Index(string message = null)
        {
            ViewData["Title"] = "Menu & Inventory Management";
            ViewBag.Message = message;
            var items = await _stockService.GetAllAsync();
            return View(items);
        }


        [HttpPost]
        public async Task<IActionResult> Add(string name, string category, decimal price, string availability, int currentStock, string unit, int reorderLevel, IFormFile imageFile)
        {
            if (!string.IsNullOrEmpty(name))
            {
                string imagePath = null;
                
                // Handle image upload if provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    imagePath = await SaveImageFile(imageFile);
                }
                
                // Set default image based on category if no image uploaded
                if (string.IsNullOrEmpty(imagePath))
                {
                    imagePath = category switch
                    {
                        "Wings" => "/images/wings.png",
                        "Appetizer" => "/images/appetize.png",
                        "Add Ons" => "/images/wings.png",
                        _ => "/images/wings.png"
                    };
                }

                var newItem = new InventoryItem
                {
                    Item = name,
                    Category = category,
                    Price = price,
                    Availability = currentStock == 0 ? "Unavailable" : (availability ?? "Available"),
                    Image = imagePath,
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
        public async Task<IActionResult> Edit(InventoryItem updated, IFormFile imageFile)
        {
            var existing = await _stockService.GetByIdAsync(updated.Id);
            if (existing != null)
            {
                // Handle image upload if a new image is provided
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Save new image
                    var newImagePath = await SaveImageFile(imageFile);
                    if (!string.IsNullOrEmpty(newImagePath))
                    {
                        updated.Image = newImagePath;
                    }
                }
                else
                {
                    // Preserve the existing image if no new image uploaded
                    updated.Image = existing.Image ?? updated.Image;
                }
                
                // Set default image based on category if image is still empty
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
