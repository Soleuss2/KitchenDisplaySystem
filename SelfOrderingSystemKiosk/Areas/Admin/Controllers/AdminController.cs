using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SelfOrderingSystemKiosk.Areas.Customer.Models;
using SelfOrderingSystemKiosk.Services;

namespace SelfOrderingSystemKiosk.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}