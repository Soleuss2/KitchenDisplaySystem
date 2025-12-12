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

        public IActionResult Index()
        {
            // Clear session when starting a new order (new session starts)
            HttpContext.Session.Remove("FirstOrderTime");
            return View();
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
            return View();
        }

        [HttpPost]
        public IActionResult SelectExperience(string experienceType)
        {
            TempData["ExperienceType"] = experienceType;
            if (experienceType == "Unlimited") return RedirectToAction("UnlimitedMenu");
            if (experienceType == "AlaCarte") return RedirectToAction("AlaCarteMenu");
            return RedirectToAction("ChooseExperience");
        }

        public async Task<IActionResult> AlaCarteMenu(bool isReorder = false, string previousOrderNumber = null)
        {
            // Set experience type and keep it for the next request
            TempData["ExperienceType"] = "AlaCarte";
            TempData.Keep("ExperienceType");
            TempData.Keep("DiningType"); // Keep dining type if it exists
            ViewBag.ExperienceType = "AlaCarte";
            ViewBag.IsReorder = isReorder;
            // Only show available items from Stock collection
            var items = await _stockService.GetAvailableAsync() ?? new List<InventoryItem>();
            return View(items);
        }

        public async Task<IActionResult> UnlimitedMenu(bool isReorder = false, string previousOrderNumber = null)
        {
            // Set experience type and keep it for the next request
            TempData["ExperienceType"] = "Unlimited";
            TempData.Keep("ExperienceType");
            TempData.Keep("DiningType"); // Keep dining type if it exists
            ViewBag.ExperienceType = "Unlimited";
            ViewBag.IsReorder = isReorder;
            
            // For reorders, calculate personCount from previous order
            int? personCount = null;
            if (isReorder && !string.IsNullOrEmpty(previousOrderNumber))
            {
                var previousOrder = await _orderService.GetByOrderNumberAsync(previousOrderNumber);
                if (previousOrder != null && previousOrder.OrderType == "Unlimited")
                {
                    // Calculate personCount from total: total = (personCount * 377) * 1.12
                    // So personCount = total / (377 * 1.12)
                    const decimal pricePerHead = 377m;
                    const decimal taxRate = 1.12m;
                    personCount = (int)Math.Round(previousOrder.Total / (pricePerHead * taxRate));
                    ViewBag.PersonCount = personCount;
                }
            }
            
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

                // Get dining type from TempData
                string diningType = TempData["DiningType"]?.ToString() ?? "DineIn";

                // Check 1-hour time limit using session
                string sessionKey = "FirstOrderTime";
                DateTime? firstOrderTime = HttpContext.Session.GetString(sessionKey) != null 
                    ? DateTime.Parse(HttpContext.Session.GetString(sessionKey)) 
                    : null;

                if (firstOrderTime.HasValue)
                {
                    // Not the first order - check if 1 hour has passed
                    var timeSinceFirstOrder = DateTime.UtcNow - firstOrderTime.Value;
                    var oneHour = TimeSpan.FromHours(1);
                    
                    if (timeSinceFirstOrder > oneHour)
                    {
                        return Json(new { 
                            success = false, 
                            message = $"Time limit exceeded. Your 1-hour session started at {firstOrderTime.Value.ToLocalTime():hh:mm tt} and has expired. Please start a new session." 
                        });
                    }
                }
                else
                {
                    // First order - store the timestamp in session
                    HttpContext.Session.SetString(sessionKey, DateTime.UtcNow.ToString("O"));
                }

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    OrderType = experienceType, // ✅ Store the order type (AlaCarte/Unlimited)
                    DiningType = diningType, // ✅ Store the dining type (DineIn/TakeOut)
                    TableNumber = null, // ✅ Table numbers removed - all reorders through confirmation page
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

            // Preserve order type and dining type for reordering
            if (!string.IsNullOrEmpty(order.OrderType))
            {
                TempData["ExperienceType"] = order.OrderType;
            }
            if (!string.IsNullOrEmpty(order.DiningType))
            {
                TempData["DiningType"] = order.DiningType;
            }

            return View(order);
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public IActionResult GetSessionInfo()
        {
            string sessionKey = "FirstOrderTime";
            string firstOrderTimeStr = HttpContext.Session.GetString(sessionKey);
            
            if (string.IsNullOrEmpty(firstOrderTimeStr))
            {
                return Json(new { hasSession = false });
            }

            DateTime firstOrderTime;
            try
            {
                // Parse as UTC to ensure consistent timezone handling
                firstOrderTime = DateTime.Parse(firstOrderTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind);
                if (firstOrderTime.Kind == DateTimeKind.Unspecified)
                {
                    firstOrderTime = DateTime.SpecifyKind(firstOrderTime, DateTimeKind.Utc);
                }
                else if (firstOrderTime.Kind == DateTimeKind.Local)
                {
                    firstOrderTime = firstOrderTime.ToUniversalTime();
                }
            }
            catch
            {
                // If parsing fails, clear the session and return no session
                HttpContext.Session.Remove(sessionKey);
                return Json(new { hasSession = false });
            }

            var timeSinceFirstOrder = DateTime.UtcNow - firstOrderTime;
            var oneHour = TimeSpan.FromHours(1);
            var timeRemaining = oneHour - timeSinceFirstOrder;
            var isExpired = timeRemaining <= TimeSpan.Zero;

            // Ensure timeRemainingSeconds is never negative and never exceeds 1 hour
            int timeRemainingSeconds = 0;
            if (!isExpired && timeRemaining.TotalSeconds > 0)
            {
                // Cap at 1 hour (3600 seconds) to prevent display issues
                timeRemainingSeconds = Math.Max(0, Math.Min(3600, (int)timeRemaining.TotalSeconds));
            }

            // Calculate minutes and seconds for display
            int minutes = timeRemainingSeconds / 60;
            int seconds = timeRemainingSeconds % 60;

            return Json(new
            {
                hasSession = true,
                firstOrderTime = firstOrderTime,
                timeRemainingSeconds = timeRemainingSeconds,
                timeRemainingMinutes = minutes,
                isExpired = isExpired,
                timeRemainingFormatted = isExpired ? "00:00" : $"{minutes:D2}:{seconds:D2}"
            });
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