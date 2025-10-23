using Microsoft.AspNetCore.Mvc;
using SelfOrderingSystemKiosk.Areas.Customer.Models;
using SelfOrderingSystemKiosk.Models;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class KioskController : Controller
    {
        private readonly OrderService _orderService;
        private readonly ChickenService _chickenService;

        public KioskController(OrderService orderService, ChickenService chickenService)
        {
            _orderService = orderService;
            _chickenService = chickenService;
        }

        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult SelectDining(string diningType)
        {
            TempData["DiningType"] = diningType;
            return RedirectToAction("ChooseExperience");
        }

        public IActionResult ChooseExperience()
        {
            ViewBag.DiningType = TempData["DiningType"];
            return View();
        }

        [HttpPost]
        public IActionResult SelectExperience(string experienceType)
        {
            TempData["ExperienceType"] = experienceType;

            if (experienceType == "Unlimited") return RedirectToAction("UnlimitedMenu");
            if (experienceType == "AlaCarte") return RedirectToAction("AlaCarteMenu");

            return RedirectToAction("ChooseExperience");
        }

        public IActionResult AlaCarteMenu()
        {
            ViewBag.ExperienceType = TempData["ExperienceType"];
            return View();
        }

        public async Task<IActionResult> UnlimitedMenu()
        {
            var flavors = await _chickenService.GetAllAsync() ?? new List<ChickenFlavors>();
            return View(flavors);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmOrder([FromForm] List<OrderItem> Items)
        {
            if (Items == null || !Items.Any())
                return Json(new { success = false, message = "No items in the order" });

            decimal subtotal = Items.Sum(i => i.Price * i.Quantity);
            decimal tax = subtotal * 0.12m;
            decimal total = subtotal + tax;

            var random = new Random();
            string orderNumber = random.Next(1000, 9999).ToString();

            var order = new Order
            {
                OrderNumber = orderNumber,
                OrderDate = DateTime.Now,
                Status = "Pending",
                Subtotal = subtotal,
                Tax = tax,
                Total = total,
                Items = Items
            };

            await _orderService.CreateAsync(order);

            return Json(new { success = true, orderNumber = order.OrderNumber });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
                return RedirectToAction("Index");

            var order = await _orderService.GetByOrderNumberAsync(orderNumber);

            if (order == null)
                return RedirectToAction("Index");

            return View(order); // passes full Order to the view
        }
    }
}
