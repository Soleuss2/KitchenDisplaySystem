using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Services;
using SelfOrderingSystemKiosk.Areas.Customer.Models;
using System.Collections.Generic;
using System.Linq;
using Order = SelfOrderingSystemKiosk.Areas.Customer.Models.Order;

using Microsoft.AspNetCore.Authorization;

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

        public async Task<IActionResult> Index(string? startDate = null, string? endDate = null)
        {
            ViewData["Title"] = "Dashboard";

            // Get real statistics from database - using Stock collection for menu/inventory
            var allMenuItems = await _stockService.GetAllAsync();
            var allOrders = await _orderService.GetAllAsync();
            var allOrdersList = allOrders ?? new List<Order>();

            // Calculate statistics
            var totalMenuItems = allMenuItems?.Count ?? 0;
            var totalInventoryItems = allMenuItems?.Count ?? 0; // All menu items have inventory now

            // Build low stock list once so we can show details in the UI
            var lowStockList = allMenuItems?
                .Where(i => i.CurrentStock <= i.ReorderLevel)
                .OrderBy(i => i.Item)
                .ToList() ?? new List<SelfOrderingSystemKiosk.Models.InventoryItem>();

            var lowStockItems = lowStockList.Count;

            // Get today's sales - orders created today (using UTC date range)
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);
            
            // Filter today's orders from all orders (fetching directly from Order collection)
            var todayOrders = allOrdersList
                .Where(o => o.OrderDate >= todayStart && o.OrderDate < todayEnd)
                .ToList();
            
            var todaysSales = todayOrders.Count;
            
            // Calculate revenue - sum all order totals from today's orders
            // Use Total field directly from Order collection
            var todaysRevenue = todayOrders
                .Where(o => o.Total > 0) // Only count orders with valid totals
                .Sum(o => o.Total);

            // Revenue split by order type (AlaCarte / Unlimited)
            decimal todaysRevenueAlaCarte = todayOrders
                .Where(o => (o.OrderType ?? "AlaCarte") == "AlaCarte" && o.Total > 0)
                .Sum(o => o.Total);

            decimal todaysRevenueUnlimited = todayOrders
                .Where(o => o.OrderType == "Unlimited" && o.Total > 0)
                .Sum(o => o.Total);

            // Revenue split by dining type (DineIn / TakeOut)
            decimal todaysRevenueDineIn = todayOrders
                .Where(o => (o.DiningType ?? "DineIn") == "DineIn" && o.Total > 0)
                .Sum(o => o.Total);

            decimal todaysRevenueTakeOut = todayOrders
                .Where(o => o.DiningType == "TakeOut" && o.Total > 0)
                .Sum(o => o.Total);

            // Time-range revenue filter - default to this week if no dates provided
            DateTime defaultRangeStart, defaultRangeEnd;
            if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
            {
                // Default to this week
                var now = DateTime.UtcNow;
                var dayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)now.DayOfWeek;
                defaultRangeStart = now.Date.AddDays(-(dayOfWeek - 1));
                defaultRangeEnd = defaultRangeStart.AddDays(7);
            }
            else
            {
                defaultRangeStart = todayStart;
                defaultRangeEnd = todayEnd;
            }
            
            var rangeStart = ParseDateOrDefault(startDate, defaultRangeStart);
            var rangeEnd = ParseDateOrDefault(endDate, defaultRangeEnd);

            // Ensure rangeEnd is after rangeStart
            if (rangeEnd <= rangeStart)
            {
                rangeEnd = rangeStart.AddDays(1);
            }

            var rangeOrders = allOrdersList
                .Where(o => o.OrderDate >= rangeStart && o.OrderDate < rangeEnd)
                .ToList();

            var rangeRevenue = rangeOrders.Where(o => o.Total > 0).Sum(o => o.Total);
            var rangeRevenueAlaCarte = rangeOrders
                .Where(o => (o.OrderType ?? "AlaCarte") == "AlaCarte" && o.Total > 0)
                .Sum(o => o.Total);
            var rangeRevenueUnlimited = rangeOrders
                .Where(o => o.OrderType == "Unlimited" && o.Total > 0)
                .Sum(o => o.Total);
            
            // Revenue split by dining type for date range
            var rangeRevenueDineIn = rangeOrders
                .Where(o => (o.DiningType ?? "DineIn") == "DineIn" && o.Total > 0)
                .Sum(o => o.Total);
            var rangeRevenueTakeOut = rangeOrders
                .Where(o => o.DiningType == "TakeOut" && o.Total > 0)
                .Sum(o => o.Total);
            
            var rangeOrderCount = rangeOrders.Count;

            // Best sellers - all time, today, and this month
            var bestSellersAllTime = BuildBestSellers(allOrdersList);
            var bestSellersToday = BuildBestSellers(todayOrders);
            
            // Monthly best sellers - orders from current month
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var monthOrders = allOrdersList
                .Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd)
                .ToList();
            var bestSellersMonthly = BuildBestSellers(monthOrders);

            // Calculate revenue for chart based on date range
            // Group orders by day within the selected range for the chart
            var chartData = new Dictionary<string, decimal>();
            if (rangeOrders.Any())
            {
                var ordersByDay = rangeOrders
                    .Where(o => o.Total > 0)
                    .GroupBy(o => o.OrderDate.Date.ToString("yyyy-MM-dd"))
                    .OrderBy(g => g.Key);

                foreach (var dayGroup in ordersByDay)
                {
                    chartData[dayGroup.Key] = dayGroup.Sum(o => o.Total);
                }
            }

            // Pass individual stats to view for stat cards
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
            ViewBag.RangeStart = rangeStart;
            ViewBag.RangeEnd = rangeEnd.AddDays(-1); // store inclusive end date for display/input
            ViewBag.RangeRevenue = rangeRevenue;
            ViewBag.RangeRevenueAlaCarte = rangeRevenueAlaCarte;
            ViewBag.RangeRevenueUnlimited = rangeRevenueUnlimited;
            ViewBag.RangeRevenueDineIn = rangeRevenueDineIn;
            ViewBag.RangeRevenueTakeOut = rangeRevenueTakeOut;
            ViewBag.RangeOrderCount = rangeOrderCount;
            ViewBag.ChartData = chartData;
            ViewBag.BestSellersAllTime = bestSellersAllTime;
            ViewBag.BestSellersToday = bestSellersToday;
            ViewBag.BestSellersMonthly = bestSellersMonthly;
            ViewBag.HasCustomRange = !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate);

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
                {
                    return Json(new { success = false, message = "Item not found." });
                }

                item.CurrentStock += quantity;
                item.Status = item.CurrentStock <= item.ReorderLevel ? "Low Stock" : "In Stock";
                item.Availability = item.CurrentStock == 0 ? "Unavailable" : "Available";

                await _stockService.UpdateAsync(item);
                return Json(new { success = true, message = $"Successfully restocked {item.Item} by {quantity}. New stock: {item.CurrentStock}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private static List<BestSeller> BuildBestSellers(IEnumerable<Order> orders)
        {
            return orders
                .SelectMany(o => o.Items ?? new List<OrderItem>())
                .Where(i => !string.IsNullOrEmpty(i.ItemName))
                .GroupBy(i => i.ItemName ?? string.Empty)
                .Select(g => new BestSeller
                {
                    ItemName = g.Key ?? string.Empty,
                    Quantity = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Price * i.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ThenByDescending(x => x.Revenue)
                .Take(5)
                .ToList();
        }

        private static DateTime ParseDateOrDefault(string? date, DateTime fallbackUtcDate)
        {
            if (DateTime.TryParse(date, out var parsed))
            {
                // Treat as UTC date without time; add Date to normalize
                return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
            }

            return fallbackUtcDate;
        }
    }
}

namespace SelfOrderingSystemKiosk.Controllers
{
    public class SalesData
    {
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class BestSeller
    {
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }
}
