using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    public class OrdersController : Controller
    {
        // Temporary in-memory list (replace with DB later)
        private static List<Order> _orders = new List<Order>
        {
            new Order { Id = 1, Customer = "John Doe", Items = "Burger, Fries", Total = 180, DateTime = DateTime.Now.AddHours(-2), Status = "Pending" },
            new Order { Id = 2, Customer = "Jane Smith", Items = "Iced Coffee", Total = 80, DateTime = DateTime.Now.AddHours(-1), Status = "Completed" },
            new Order { Id = 3, Customer = "Mark Cruz", Items = "Burger, Soda", Total = 150, DateTime = DateTime.Now, Status = "Pending" }
        };

        public IActionResult Index()
        {
            return View(_orders);
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                order.Status = status;
                TempData["Message"] = $"Order #{id} status updated to '{status}'.";
            }
            return RedirectToAction("Index");
        }
    }
}
