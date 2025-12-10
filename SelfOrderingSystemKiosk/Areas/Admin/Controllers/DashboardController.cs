using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Services;
using SelfOrderingSystemKiosk.Areas.Customer.Models;
using System.Collections.Generic;
using System.Linq;
using Order = SelfOrderingSystemKiosk.Areas.Customer.Models.Order;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
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

            // Time-range revenue filter
            var rangeStart = ParseDateOrDefault(startDate, todayStart);
            var rangeEnd = ParseDateOrDefault(endDate, todayEnd);

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
            var rangeOrderCount = rangeOrders.Count;

            // Best sellers - all time and today
            var bestSellersAllTime = BuildBestSellers(allOrdersList);
            var bestSellersToday = BuildBestSellers(todayOrders);

            // Calculate sales summaries by day, month, and year
            var currentYear = DateTime.UtcNow.Year;
            var dailySales = new Dictionary<string, SalesData>();
            var monthlySales = new Dictionary<string, SalesData>();
            var yearlySales = new Dictionary<int, SalesData>();

            if (allOrdersList.Any())
            {
                // Group orders by day (last 30 days)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;
                var ordersByDay = allOrdersList
                    .Where(o => o.OrderDate >= thirtyDaysAgo)
                    .GroupBy(o => o.OrderDate.Date.ToString("yyyy-MM-dd"))
                    .OrderBy(g => g.Key);

                foreach (var dayGroup in ordersByDay)
                {
                    dailySales[dayGroup.Key] = new SalesData
                    {
                        Count = dayGroup.Count(),
                        Revenue = dayGroup.Sum(o => o.Total)
                    };
                }

                // Group orders by month (last 12 months)
                var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
                var ordersByMonth = allOrdersList
                    .Where(o => o.OrderDate >= twelveMonthsAgo)
                    .GroupBy(o => o.OrderDate.ToString("yyyy-MM"))
                    .OrderBy(g => g.Key);

                foreach (var monthGroup in ordersByMonth)
                {
                    monthlySales[monthGroup.Key] = new SalesData
                    {
                        Count = monthGroup.Count(),
                        Revenue = monthGroup.Sum(o => o.Total)
                    };
                }

                // Group orders by year (last 5 years)
                var ordersByYear = allOrdersList
                    .Where(o => o.OrderDate.Year >= currentYear - 4)
                    .GroupBy(o => o.OrderDate.Year)
                    .OrderBy(g => g.Key);

                foreach (var yearGroup in ordersByYear)
                {
                    yearlySales[yearGroup.Key] = new SalesData
                    {
                        Count = yearGroup.Count(),
                        Revenue = yearGroup.Sum(o => o.Total)
                    };
                }
            }

            // Data for the bar chart
            ViewBag.Labels = new[] { "Menu Items", "Inventory Items", "Low Stock", "Today's Sales" };
            ViewBag.Values = new[] { totalMenuItems, totalInventoryItems, lowStockItems, todaysSales };

            // Pass individual stats to view for stat cards
            ViewBag.TotalMenuItems = totalMenuItems;
            ViewBag.TotalInventoryItems = totalInventoryItems;
            ViewBag.LowStockItems = lowStockItems;
            ViewBag.LowStockList = lowStockList;
            ViewBag.TodaysSales = todaysSales;
            ViewBag.TodaysRevenue = todaysRevenue;
            ViewBag.TodaysRevenueAlaCarte = todaysRevenueAlaCarte;
            ViewBag.TodaysRevenueUnlimited = todaysRevenueUnlimited;
            ViewBag.RangeStart = rangeStart;
            ViewBag.RangeEnd = rangeEnd.AddDays(-1); // store inclusive end date for display/input
            ViewBag.RangeRevenue = rangeRevenue;
            ViewBag.RangeRevenueAlaCarte = rangeRevenueAlaCarte;
            ViewBag.RangeRevenueUnlimited = rangeRevenueUnlimited;
            ViewBag.RangeOrderCount = rangeOrderCount;
            ViewBag.DailySales = dailySales;
            ViewBag.MonthlySales = monthlySales;
            ViewBag.YearlySales = yearlySales;
            ViewBag.BestSellersAllTime = bestSellersAllTime;
            ViewBag.BestSellersToday = bestSellersToday;

            return View();
        }

        private static List<BestSeller> BuildBestSellers(IEnumerable<Order> orders)
        {
            return orders
                .SelectMany(o => o.Items ?? new List<OrderItem>())
                .GroupBy(i => i.ItemName)
                .Select(g => new BestSeller
                {
                    ItemName = g.Key,
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
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }
}
