using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Doctor.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Doctor)]
    [Area("Doctor")]
    [Route("Doctor/Dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("Areas/Doctor/Views/Dashboard/Index.cshtml");
        }
    }
}
