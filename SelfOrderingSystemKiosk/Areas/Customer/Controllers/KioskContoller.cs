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

        public async Task<IActionResult> AlaCarteMenu()
        {
            TempData.Keep("ExperienceType"); // Keep it for the next request
            ViewBag.ExperienceType = "AlaCarte";
            // Only show available items from Stock collection
            var items = await _stockService.GetAvailableAsync() ?? new List<InventoryItem>();
            return View(items);
        }

        public async Task<IActionResult> UnlimitedMenu()
        {
            TempData.Keep("ExperienceType"); // Keep it for the next request
            ViewBag.ExperienceType = "Unlimited";
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

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    OrderType = experienceType, // ✅ Store the order type
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
    }
}