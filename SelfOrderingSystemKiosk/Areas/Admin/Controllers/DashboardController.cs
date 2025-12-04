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

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";

            // Get real statistics from database - using Stock collection for menu/inventory
            var allMenuItems = await _stockService.GetAllAsync();
            var allOrders = await _orderService.GetAllAsync();
            var allOrdersList = allOrders ?? new List<Order>();

            // Calculate statistics
            var totalMenuItems = allMenuItems?.Count ?? 0;
            var totalInventoryItems = allMenuItems?.Count ?? 0; // All menu items have inventory now
            var lowStockItems = allMenuItems?.Count(i => i.CurrentStock <= i.ReorderLevel) ?? 0;

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
            ViewBag.TodaysSales = todaysSales;
            ViewBag.TodaysRevenue = todaysRevenue;
            ViewBag.DailySales = dailySales;
            ViewBag.MonthlySales = monthlySales;
            ViewBag.YearlySales = yearlySales;

            return View();
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
}
