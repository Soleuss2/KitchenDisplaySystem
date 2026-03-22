using System;
using System.Collections.Generic;
using System.Linq;
using SelfOrderingSystemKiosk.Models;

namespace SelfOrderingSystemKiosk.Services
{
    public static class OrderSalesAnalytics
    {
        public static List<BestSeller> BuildBestSellers(
            IEnumerable<SelfOrderingSystemKiosk.Areas.Customer.Models.Order> orders,
            int take = 5)
        {
            return orders
                .SelectMany(o => o.Items ?? new List<SelfOrderingSystemKiosk.Areas.Customer.Models.OrderItem>())
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
                .Take(take)
                .ToList();
        }

        public static DateTime ParseDateOrDefault(string? date, DateTime fallbackUtcDate)
        {
            if (DateTime.TryParse(date, out var parsed))
                return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
            return fallbackUtcDate;
        }
    }
}
