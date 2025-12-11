using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Areas.Customer.Models;
using SelfOrderingSystemKiosk.Models;
using SelfOrderingSystemKiosk.Services;
using Order = SelfOrderingSystemKiosk.Areas.Customer.Models.Order;

namespace SelfOrderingSystemKiosk.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class KioskController : Controller
    {
        private readonly OrderService _orderService;
        private readonly StockService _stockService;

        public KioskController(OrderService orderService, StockService stockService)
        {
            _orderService = orderService;
            _stockService = stockService;
        }

        public IActionResult Index() => View();

        // QR Code route - redirects to menu with table number
        public IActionResult TableMenu([FromQuery] string tableNumber)
        {
            if (string.IsNullOrEmpty(tableNumber))
            {
                return RedirectToAction("Index");
            }

            // Set table number in TempData and redirect to experience selection
            TempData["TableNumber"] = tableNumber;
            TempData["DiningType"] = "DineIn"; // Table orders are always Dine In
            return RedirectToAction("ChooseExperience");
        }

        [HttpPost]
        public IActionResult SelectDining(string diningType)
        {
            TempData["DiningType"] = diningType;

            if (diningType == "TakeOut")
            {
                // Skip experience selection and go straight to Ala Carte menu
                return RedirectToAction("AlaCarteMenu");
            }

            // Dine In goes to experience selection
            return RedirectToAction("ChooseExperience");
        }

        public IActionResult ChooseExperience()
        {
            ViewBag.DiningType = TempData["DiningType"];
            ViewBag.TableNumber = TempData["TableNumber"];
            // Keep table number in TempData for next request
            if (TempData["TableNumber"] != null)
            {
                TempData.Keep("TableNumber");
            }
            return View();
        }

        [HttpPost]
        public IActionResult SelectExperience(string experienceType)
        {
            TempData["ExperienceType"] = experienceType;
            // Preserve table number if it exists
            if (TempData["TableNumber"] != null)
            {
                TempData.Keep("TableNumber");
            }
            if (experienceType == "Unlimited") return RedirectToAction("UnlimitedMenu");
            if (experienceType == "AlaCarte") return RedirectToAction("AlaCarteMenu");
            return RedirectToAction("ChooseExperience");
        }

        public async Task<IActionResult> AlaCarteMenu(string tableNumber = null)
        {
            TempData.Keep("ExperienceType"); // Keep it for the next request
            if (!string.IsNullOrEmpty(tableNumber))
            {
                TempData["TableNumber"] = tableNumber;
            }
            else if (TempData["TableNumber"] != null)
            {
                TempData.Keep("TableNumber"); // Keep table number if it exists
            }
            ViewBag.ExperienceType = "AlaCarte";
            ViewBag.TableNumber = tableNumber ?? TempData["TableNumber"]?.ToString();
            // Only show available items from Stock collection
            var items = await _stockService.GetAvailableAsync() ?? new List<InventoryItem>();
            return View(items);
        }

        public async Task<IActionResult> UnlimitedMenu(string tableNumber = null)
        {
            TempData.Keep("ExperienceType"); // Keep it for the next request
            if (!string.IsNullOrEmpty(tableNumber))
            {
                TempData["TableNumber"] = tableNumber;
            }
            else if (TempData["TableNumber"] != null)
            {
                TempData.Keep("TableNumber"); // Keep table number if it exists
            }
            ViewBag.ExperienceType = "Unlimited";
            ViewBag.TableNumber = tableNumber ?? TempData["TableNumber"]?.ToString();
            // Only show available items from Stock collection
            var items = await _stockService.GetAvailableAsync() ?? new List<InventoryItem>();
            return View(items);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // Allow API calls without CSRF token
        public async Task<IActionResult> ConfirmOrder([FromBody] List<OrderItem> Items, [FromQuery] string orderType, [FromQuery] int? personCount)
        {
            try
            {
                // Log incoming request for debugging
                Console.WriteLine($"ConfirmOrder called - orderType: {orderType}, personCount: {personCount}");
                Console.WriteLine($"Items count: {Items?.Count ?? 0}");
                
                if (Items == null || !Items.Any())
                    return Json(new { success = false, message = "No items in the order" });

                // Get orderType from TempData if not in query string
                string experienceType = orderType ?? TempData["ExperienceType"]?.ToString() ?? "AlaCarte";

                // Validate quantity limit for Ala Carte orders (max 5 per item)
                if (experienceType == "AlaCarte")
                {
                    var itemsExceedingLimit = Items.Where(i => i.Quantity > 5).ToList();
                    if (itemsExceedingLimit.Any())
                    {
                        var itemNames = string.Join(", ", itemsExceedingLimit.Select(i => i.ItemName));
                        return Json(new { success = false, message = $"Maximum quantity of 5 per item allowed. The following items exceed this limit: {itemNames}" });
                    }
                }

                decimal subtotal;
                decimal tax;
                decimal total;

                // For Unlimited orders, calculate based on personCount * pricePerHead
                if (experienceType == "Unlimited" && personCount.HasValue && personCount.Value > 0)
                {
                    const decimal pricePerHead = 377m;
                    subtotal = personCount.Value * pricePerHead;
                    tax = subtotal * 0.12m;
                    total = subtotal + tax;
                }
                else
                {
                    // For Ala Carte orders, calculate based on item prices
                    subtotal = Items.Sum(i => i.Price * i.Quantity);
                    tax = subtotal * 0.12m;
                    total = subtotal + tax;
                }

                var random = new Random();
                string orderNumber = random.Next(1000, 9999).ToString();

                // Get dining type and table number from TempData
                string diningType = TempData["DiningType"]?.ToString() ?? "DineIn";
                string tableNumber = TempData["TableNumber"]?.ToString();
                
                // Keep table number in TempData in case user wants to reorder
                if (!string.IsNullOrEmpty(tableNumber))
                {
                    TempData.Keep("TableNumber");
                }

                // Check 1-hour time limit for DineIn table orders
                if (!string.IsNullOrEmpty(tableNumber) && diningType == "DineIn")
                {
                    var existingOrders = await _orderService.GetOrdersByTableAsync(tableNumber);
                    
                    if (existingOrders.Any())
                    {
                        // Not the first order - check if 1 hour has passed since first order
                        var firstOrder = existingOrders.OrderBy(o => o.OrderDate).First();
                        var timeSinceFirstOrder = DateTime.UtcNow - firstOrder.OrderDate;
                        var oneHour = TimeSpan.FromHours(1);
                        
                        if (timeSinceFirstOrder > oneHour)
                        {
                            return Json(new { 
                                success = false, 
                                message = $"Time limit exceeded. This table's 1-hour session started at {firstOrder.OrderDate:hh:mm tt} and has expired. Please contact staff for assistance." 
                            });
                        }
                    }
                    // If it's the first order, allow it (timer starts now)
                }

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    OrderType = experienceType, // ✅ Store the order type (AlaCarte/Unlimited)
                    DiningType = diningType, // ✅ Store the dining type (DineIn/TakeOut)
                    TableNumber = tableNumber, // ✅ Store the table number if available
                    Subtotal = subtotal,
                    Tax = tax,
                    Total = total,
                    Items = Items
                };

                await _orderService.CreateAsync(order);

                return Json(new { success = true, orderNumber = order.OrderNumber });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                Console.WriteLine($"Error creating order: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error creating order: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
                return RedirectToAction("Index");

            var order = await _orderService.GetByOrderNumberAsync(orderNumber);
            if (order == null)
                return RedirectToAction("Index");

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
                return Json(new { status = "" });

            var order = await _orderService.GetByOrderNumberAsync(orderNumber);
            if (order == null)
                return Json(new { status = "" });

            return Json(new { status = order.Status });
        }

        [HttpGet]
        public async Task<IActionResult> LookupOrder(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                TempData["ErrorMessage"] = "Please enter an order number";
                return RedirectToAction("Index");
            }

            var order = await _orderService.GetByOrderNumberAsync(orderNumber);
            if (order == null)
            {
                TempData["ErrorMessage"] = $"Order #{orderNumber} not found. Please check your order number and try again.";
                return RedirectToAction("Index");
            }

            // Order found, redirect to confirmation page
            return RedirectToAction("Confirmation", new { orderNumber = orderNumber });
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetTableSessionInfo([FromQuery] string tableNumber)
        {
            if (string.IsNullOrEmpty(tableNumber))
            {
                return Json(new { hasSession = false });
            }

            var existingOrders = await _orderService.GetOrdersByTableAsync(tableNumber);
            
            if (!existingOrders.Any())
            {
                return Json(new { hasSession = false });
            }

            var firstOrder = existingOrders.OrderBy(o => o.OrderDate).First();
            var timeSinceFirstOrder = DateTime.UtcNow - firstOrder.OrderDate;
            var oneHour = TimeSpan.FromHours(1);
            var timeRemaining = oneHour - timeSinceFirstOrder;
            var isExpired = timeRemaining <= TimeSpan.Zero;

            return Json(new
            {
                hasSession = true,
                firstOrderTime = firstOrder.OrderDate,
                timeRemainingSeconds = isExpired ? 0 : (int)timeRemaining.TotalSeconds,
                timeRemainingMinutes = isExpired ? 0 : (int)timeRemaining.TotalMinutes,
                isExpired = isExpired,
                timeRemainingFormatted = isExpired ? "00:00" : $"{(int)timeRemaining.TotalMinutes:D2}:{timeRemaining.Seconds:D2}"
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // Allow API calls without CSRF token
        public async Task<IActionResult> CancelOrder(string orderNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(orderNumber))
                {
                    return Json(new { success = false, message = "Order number is required" });
                }

                var order = await _orderService.GetByOrderNumberAsync(orderNumber);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Check if order can be cancelled (not in progress, completed, or already cancelled)
                if (order.Status != null && 
                    (order.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase) ||
                     order.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                     order.Status.Equals("Canceled", StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new { success = false, message = $"Cannot cancel order. Order status is: {order.Status}" });
                }

                // Cancel the order
                await _orderService.CancelOrderAsync(order.Id);

                return Json(new { success = true, message = "Order cancelled successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cancelling order: {ex.Message}");
                return Json(new { success = false, message = $"Error cancelling order: {ex.Message}" });
            }
        }
    }
}