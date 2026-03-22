using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Kitchen")]
    public class SalesController : Controller
    {
        private readonly OrderService _orderService;

        public SalesController(OrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> Index(string? startDate = null, string? endDate = null)
        {
            ViewData["Title"] = "Sales & reports";

            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1);
            var todayOrders = await _orderService.GetByDateRangeHalfOpenAsync(todayStart, todayEnd);

            DateTime defaultRangeStart, defaultRangeEnd;
            if (string.IsNullOrEmpty(startDate) && string.IsNullOrEmpty(endDate))
            {
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

            var rangeStart = OrderSalesAnalytics.ParseDateOrDefault(startDate, defaultRangeStart);
            var rangeEnd = OrderSalesAnalytics.ParseDateOrDefault(endDate, defaultRangeEnd);
            if (rangeEnd <= rangeStart)
                rangeEnd = rangeStart.AddDays(1);

            var rangeOrders = await _orderService.GetByDateRangeHalfOpenAsync(rangeStart, rangeEnd);

            var rangeRevenue = rangeOrders.Where(o => o.Total > 0).Sum(o => o.Total);
            var rangeRevenueAlaCarte = rangeOrders
                .Where(o => (o.OrderType ?? "AlaCarte") == "AlaCarte" && o.Total > 0)
                .Sum(o => o.Total);
            var rangeRevenueUnlimited = rangeOrders
                .Where(o => o.OrderType == "Unlimited" && o.Total > 0)
                .Sum(o => o.Total);
            var rangeRevenueDineIn = rangeOrders
                .Where(o => (o.DiningType ?? "DineIn") == "DineIn" && o.Total > 0)
                .Sum(o => o.Total);
            var rangeRevenueTakeOut = rangeOrders
                .Where(o => o.DiningType == "TakeOut" && o.Total > 0)
                .Sum(o => o.Total);
            var rangeOrderCount = rangeOrders.Count;

            var historyStart = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var allOrdersForBestSellers = await _orderService.GetByDateRangeHalfOpenAsync(historyStart, DateTime.UtcNow.AddDays(1));
            var bestSellersAllTime = OrderSalesAnalytics.BuildBestSellers(allOrdersForBestSellers);
            var bestSellersToday = OrderSalesAnalytics.BuildBestSellers(todayOrders);

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            var monthOrders = await _orderService.GetByDateRangeHalfOpenAsync(monthStart, monthEnd);
            var bestSellersMonthly = OrderSalesAnalytics.BuildBestSellers(monthOrders);

            var chartData = new Dictionary<string, decimal>();
            if (rangeOrders.Any())
            {
                var ordersByDay = rangeOrders
                    .Where(o => o.Total > 0)
                    .GroupBy(o => o.OrderDate.Date.ToString("yyyy-MM-dd"))
                    .OrderBy(g => g.Key);
                foreach (var dayGroup in ordersByDay)
                    chartData[dayGroup.Key] = dayGroup.Sum(o => o.Total);
            }

            ViewBag.RangeStart = rangeStart;
            ViewBag.RangeEnd = rangeEnd.AddDays(-1);
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
    }
}
