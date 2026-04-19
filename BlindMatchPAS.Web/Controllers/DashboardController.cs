using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlindMatchPAS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole("Student"))
        {
            return RedirectToAction("Index", "Student");
        }

        if (User.IsInRole("Supervisor"))
        {
            return RedirectToAction("Index", "Supervisor");
        }

        if (User.IsInRole("ModuleLeader"))
        {
            return RedirectToAction("Index", "ModuleLeader");
        }

        if (User.IsInRole("SystemAdministrator"))
        {
            return RedirectToAction("Index", "SystemAdmin");
        }

        return RedirectToAction("AccessDenied", "Home");
    }
}
