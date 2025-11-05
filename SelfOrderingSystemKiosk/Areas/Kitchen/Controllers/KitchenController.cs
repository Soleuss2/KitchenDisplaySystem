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

        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetAllAsync();
            return View(orders.OrderByDescending(o => o.OrderDate)); // latest orders first
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
