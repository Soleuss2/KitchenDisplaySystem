using Microsoft.AspNetCore.Mvc;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Dashboard";

            // Sample data for the bar chart 
            ViewBag.Labels = new[] { "Menu Items", "Inventory", "Low Stock", "Today's Sales" };
            ViewBag.Values = new[] { 24, 58, 4, 12 }; 

            return View();
        }
    }
}
