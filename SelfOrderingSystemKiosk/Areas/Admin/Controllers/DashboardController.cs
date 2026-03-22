using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Kitchen")]
    public class DashboardController : Controller
    {
        private readonly StockService _stockService;
        private readonly OrderService _orderService;

        public DashboardController(StockService stockService, OrderService orderService)
        {
            _stockService = stockService;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Admin dashboard";

            var allMenuItems = await _stockService.GetAllAsync();

            var totalMenuItems = allMenuItems?.Count ?? 0;
            var totalInventoryItems = allMenuItems?.Count ?? 0;

            var lowStockList = allMenuItems?
                .Where(i => i.CurrentStock <= i.ReorderLevel)
                .OrderBy(i => i.Item)
                .ToList() ?? new List<SelfOrderingSystemKiosk.Models.InventoryItem>();

            var lowStockItems = lowStockList.Count;

            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);
            var todayOrders = await _orderService.GetByDateRangeHalfOpenAsync(todayStart, todayEnd);

            var todaysSales = todayOrders.Count;
            var todaysRevenue = todayOrders.Where(o => o.Total > 0).Sum(o => o.Total);
            var todaysRevenueAlaCarte = todayOrders
                .Where(o => (o.OrderType ?? "AlaCarte") == "AlaCarte" && o.Total > 0)
                .Sum(o => o.Total);
            var todaysRevenueUnlimited = todayOrders
                .Where(o => o.OrderType == "Unlimited" && o.Total > 0)
                .Sum(o => o.Total);
            var todaysRevenueDineIn = todayOrders
                .Where(o => (o.DiningType ?? "DineIn") == "DineIn" && o.Total > 0)
                .Sum(o => o.Total);
            var todaysRevenueTakeOut = todayOrders
                .Where(o => o.DiningType == "TakeOut" && o.Total > 0)
                .Sum(o => o.Total);

            ViewBag.TotalMenuItems = totalMenuItems;
            ViewBag.TotalInventoryItems = totalInventoryItems;
            ViewBag.LowStockItems = lowStockItems;
            ViewBag.LowStockList = lowStockList;
            ViewBag.TodaysSales = todaysSales;
            ViewBag.TodaysRevenue = todaysRevenue;
            ViewBag.TodaysRevenueAlaCarte = todaysRevenueAlaCarte;
            ViewBag.TodaysRevenueUnlimited = todaysRevenueUnlimited;
            ViewBag.TodaysRevenueDineIn = todaysRevenueDineIn;
            ViewBag.TodaysRevenueTakeOut = todaysRevenueTakeOut;

            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RestockItem(string id, int quantity)
        {
            try
            {
                var item = await _stockService.GetByIdAsync(id);
                if (item == null)
                    return Json(new { success = false, message = "Item not found." });

                var ok = await _stockService.IncreaseStockAsync(id, quantity, "Dashboard", null, "Dashboard restock");
                if (!ok)
                    return Json(new { success = false, message = "Could not restock." });

                var updated = await _stockService.GetByIdAsync(id);
                return Json(new { success = true, message = $"Successfully restocked {updated?.Item} by {quantity}. New stock: {updated?.CurrentStock}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
