using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicalSystem.WebUI.Areas.Doctor.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Doctor)]
    [Area("Doctor")]
    [Route("Doctor/Clinic")]
    public class ClinicController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public ClinicController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetAllClinics")]
        public async Task<IActionResult> GetAllClinics(
            [FromQuery] string searchString = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound();
            }

            int pageSize = 6;
            var (clinics, totalItems) =
                await _unitOfWork.Clinic.GetFilteredClinicsAsync(
                    doctor.Id, searchString, currentSort, currentPage, pageSize);

            var clinicRows = new List<ClinicRowVM>();
            foreach (var c in clinics)
            {
                clinicRows.Add(new ClinicRowVM
                {
                    Clinic = c,
                });
            }

            var vm = new ClinicListVM
            {
                Clinics = clinicRows,
                SourceView = "GetAllClinics",

                SearchString = searchString,

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Doctor/Views/Clinic/GetAllClinics.cshtml", vm);
        }

        [HttpGet("GetClinic/{id:int}")]
        public async Task<IActionResult> GetClinic(
            [FromRoute] int id, [FromQuery] ClinicVM vm)
        {
            var clinic = await _unitOfWork.Clinic.GetFirstOrDefaultAsync(
                c => c.Id == id);
            if (clinic == null)
            {
                return NotFound();
            }

            vm.Clinic = clinic;
            vm.SourceView = "GetClinic";

            return View("Areas/Doctor/Views/Clinic/GetClinic.cshtml", vm);
        }

        [HttpGet("CreateClinic")]
        public IActionResult CreateClinic([FromQuery] ClinicVM vm)
        {
            return View("Areas/Doctor/Views/Clinic/CreateClinic.cshtml", vm);
        }

        [HttpPost("CreateClinic")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClinicConfirmed([FromForm] ClinicVM vm)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound();
            }

            if (await _unitOfWork.Clinic.IsNameExistsAsync(vm.Clinic.Name))
            {
                ModelState.AddModelError("Clinic.Name", "This Name is already exist");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Doctor/Views/Clinic/CreateClinic.cshtml", vm);
            }

            vm.Clinic.DoctorId = doctor.Id;

            await _unitOfWork.Clinic.AddAsync(vm.Clinic);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Clinic Created",
                Message = $"Dear Dr. {doctor.FullName}, your new clinic \"{vm.Clinic.Name}\" was created successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_System, 
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Clinic created successfully.";
            return RedirectToAction("GetAllClinics", "Clinic", new { area = "Doctor" });
        }

        [HttpGet("EditClinic/{id:int}")]
        public async Task<IActionResult> EditClinic(
            [FromRoute] int id, [FromQuery] ClinicVM vm)
        {
            var clinic = await _unitOfWork.Clinic.GetFirstOrDefaultAsync(
                c => c.Id == id);
            if (clinic == null)
            {
                return NotFound();
            }

            vm.Clinic = clinic;

            return View("Areas/Doctor/Views/Clinic/EditClinic.cshtml", vm);
        }

        [HttpPost("EditClinic/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClinicConfirmed([FromForm] ClinicVM vm)
        {
            if (await _unitOfWork.Clinic.IsNameExistsAsync(vm.Clinic.Name, vm.Clinic.Id))
            {
                ModelState.AddModelError("Clinic.Name", "This Name is already exist");
            }

            if (!ModelState.IsValid)
            {
                return View("Areas/Doctor/Views/Clinic/EditClinic.cshtml", vm);
            }

            await _unitOfWork.Clinic.UpdateAsync(vm.Clinic);
            await _unitOfWork.SaveAsync();

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == vm.Clinic.DoctorId);

            if (doctor != null)
            {
                var doctorNotification = new Notification
                {
                    Title = "Clinic Updated",
                    Message = $"Dear Dr. {doctor.FullName}, your clinic \"{vm.Clinic.Name}\" was updated successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                    Type = SD.Type_System, 
                    Status = SD.Status_Unreaded,
                    DoctorId = doctor.Id,
                };

                await _unitOfWork.Notification.AddAsync(doctorNotification);
                await _unitOfWork.SaveAsync();
            }

            TempData["success"] = "Clinic updated successfully.";
            return RedirectToAction(vm.SourceView, "Clinic", new
            {
                area = "Doctor",
                id = vm.Clinic.Id,
                searchString = vm.SearchString,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteClinic/{id:int}")]
        public async Task<IActionResult> DeleteClinic([FromRoute] int id, [FromQuery] ClinicVM vm)
        {
            var clinic = await _unitOfWork.Clinic.GetFirstOrDefaultAsync(
                c => c.Id == id);
            if (clinic == null)
            {
                return NotFound();
            }

            vm.Clinic = clinic;

            return View("Areas/Doctor/Views/Clinic/DeleteClinic.cshtml", vm);
        }

        [HttpPost("DeleteClinic/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClinicConfirmed([FromForm] ClinicVM vm)
        {
            var clinic = await _unitOfWork.Clinic.GetFirstOrDefaultAsync(
                c => c.Id == vm.Clinic.Id, includeProperties: "Doctor,Schedules.Appointments");

            if (clinic == null)
            {
                return NotFound();
            }

            foreach (var schedule in clinic.Schedules.ToList())
            {
                foreach (var appointment in schedule.Appointments.ToList())
                {
                    var notification = new Notification
                    {
                        Title = "Clinic Deleted",
                        Message = $"Your appointment with Dr. {clinic.Doctor.FullName} " +
                                  $"at {clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                                  $"at {appointment.AppointmentTime} has been cancelled because the clinic was deleted.",
                        Type = SD.Type_Appointment,
                        Status = SD.Status_Unreaded,
                        PatientId = appointment.PatientId
                    };

                    await _unitOfWork.Notification.AddAsync(notification);
                }
            }

            var doctorNotification = new Notification
            {
                Title = "Clinic Deleted",
                Message = $"Dear Dr. {clinic.Doctor.FullName}, your clinic \"{clinic.Name}\" " +
                  $"was deleted successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}. " +
                  $"All associated schedules and appointments have been removed.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            _unitOfWork.Clinic.Remove(clinic);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Clinic deleted successfully.";
            return RedirectToAction("GetAllClinics", "Clinic", new
            {
                area = "Doctor",
                searchString = vm.SearchString,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
