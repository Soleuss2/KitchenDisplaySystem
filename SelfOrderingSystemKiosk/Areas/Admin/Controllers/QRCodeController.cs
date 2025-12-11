using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SelfOrderingSystemKiosk.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Kitchen")]
    public class QRCodeController : Controller
    {
        public IActionResult GenerateQRCodes()
        {
            ViewData["Title"] = "Generate QR Codes for Tables";
            return View();
        }
    }
}

