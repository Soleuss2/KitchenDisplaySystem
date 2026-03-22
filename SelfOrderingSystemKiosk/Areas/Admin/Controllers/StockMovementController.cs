using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Kitchen")]
    public class StockMovementController : Controller
    {
        private readonly StockMovementService _movementService;

        public StockMovementController(StockMovementService movementService)
        {
            _movementService = movementService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? range = "7")
        {
            ViewData["Title"] = "Stock history";
            var days = range switch
            {
                "1" => 1,
                "30" => 30,
                "90" => 90,
                _ => 7
            };
            var start = DateTime.UtcNow.AddDays(-days);
            var end = DateTime.UtcNow.AddMinutes(1);
            var movements = await _movementService.GetRecentAsync(start, end, 1000);
            ViewBag.Range = range;
            ViewBag.Days = days;
            return View(movements);
        }
    }
}
