using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Areas.Kitchen.Controllers
{

    [Area("Kitchen")]
    public class KitchenController : Controller
    {
        private readonly OrderService _orderService;

        public KitchenController(OrderService orderService)
        {
            _orderService = orderService;
        }

        // GET: Kitchen/Kitchen/Index
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? dateFilter = "all")
        {
            var allOrders = await _orderService.GetAllAsync();
            IEnumerable<SelfOrderingSystemKiosk.Areas.Customer.Models.Order> orders = allOrders;

            // Apply date filter
            var now = DateTime.UtcNow;
            var filter = string.IsNullOrEmpty(dateFilter) ? "all" : dateFilter.ToLower();
            switch (filter)
            {
                case "day":
                    var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                    var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
                    orders = allOrders.Where(o => o.OrderDate >= startOfDay && o.OrderDate <= endOfDay);
                    break;
                case "week":
                    var startOfWeek = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(-(int)now.DayOfWeek);
                    var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);
                    orders = allOrders.Where(o => o.OrderDate >= startOfWeek && o.OrderDate <= endOfWeek);
                    break;
                case "month":
                    var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
                    orders = allOrders.Where(o => o.OrderDate >= startOfMonth && o.OrderDate <= endOfMonth);
                    break;
                case "all":
                default:
                    // No filtering, show all orders
                    orders = allOrders;
                    break;
            }

            ViewBag.DateFilter = dateFilter;
            return View(orders.OrderByDescending(o => o.OrderDate).ToList()); // latest orders first
        }

        // Optional: view single order
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            var order = await _orderService.GetByIdAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            return View(order);
        }

        // Optional: update status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
            await _orderService.UpdateStatusAsync(id, status);
            return RedirectToAction("Index");
        }
    }
}
