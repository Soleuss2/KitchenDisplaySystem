using Microsoft.AspNetCore.Mvc;

namespace SelfOrderingSystemKiosk.Controllers
{
    public class KioskController : Controller
    {
        // ?? GET: Kiosk Landing Page
        public IActionResult Index()
        {
            return View();
        }

        // ??? POST: Select Dining Type (Dine-In / Takeout)
        [HttpPost]
        public IActionResult SelectDining(string diningType)
        {
            // Temporarily store selected dining type
            TempData["DiningType"] = diningType;

            // Redirect to Choose Experience page
            return RedirectToAction("ChooseExperience");
        }

        // ?? GET: Choose Your Wing Experience Page
        public IActionResult ChooseExperience()
        {
            // Retrieve dining type from TempData
            ViewBag.DiningType = TempData["DiningType"];
            return View();
        }

        // ?? POST: Select Experience (Unlimited / Ala Carte)
        [HttpPost]
        public IActionResult SelectExperience(string experienceType)
        {
            // Temporarily store the selected experience type
            TempData["ExperienceType"] = experienceType;

            if (experienceType == "Unlimited")
            {
                // Redirect to the Unlimited Menu page
                return RedirectToAction("UnlimitedMenu");
            }
            else if (experienceType == "AlaCarte")
            {
                // (Placeholder) — Add AlaCarteMenu later
                return RedirectToAction("AlaCarteMenu");
            }

            // Default fallback
            return RedirectToAction("ChooseExperience");
        }

        // ?? GET: Unlimited Menu Page
        public IActionResult UnlimitedMenu()
        {
            ViewBag.ExperienceType = TempData["ExperienceType"];
            return View();
        }

        // (Optional placeholder for Ala Carte page)
        public IActionResult AlaCarteMenu()
        {
            ViewBag.ExperienceType = TempData["ExperienceType"];
            return View();
        }
    }
}
