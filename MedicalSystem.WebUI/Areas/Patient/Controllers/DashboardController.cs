using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("Patient/Dashboard")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("Areas/Patient/Views/Dashboard/Index.cshtml");
        }
    }
}
