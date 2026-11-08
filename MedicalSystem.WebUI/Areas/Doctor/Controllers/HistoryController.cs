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
    [Route("Doctor/History")]
    public class HistoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public HistoryController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("GetAllAppointments")]
        public async Task<IActionResult> GetAllAppointments(
             [FromQuery] int? clinicId = null,
             [FromQuery] string dayOfWeek = "",
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
            var (appointments, totalItems) =
                await _unitOfWork.Appointment.GetFilteredAppointmentsAsync(
                    null, doctor.Id, "", clinicId, dayOfWeek, currentSort, currentPage, pageSize,
                    new[] { SD.Status_Cancelled, SD.Status_Completed });

            var appointmentRows = new List<AppointmentRowVM>();
            foreach (var a in appointments)
            {
                appointmentRows.Add(new AppointmentRowVM
                {
                    Appointment = a,
                    ClinicName = a.Schedule.Clinic.Name,
                    AppointmentDay = a.Schedule.DayOfWeek,
                });
            }

            var vm = new AppointmentListVM
            {
                Appointments = appointmentRows,
                SourceView = "GetAllAppointments",

                ClinicId = clinicId,
                ClinicList = (await _unitOfWork.Clinic.GetAllAsync(c => c.DoctorId == doctor.Id)).Select(
                    c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = (clinicId.HasValue && clinicId.Value == c.Id)
                    }),

                DayOfWeek = dayOfWeek,
                DayOfWeekList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Saturday", Text = "Saturday", Selected = dayOfWeek == "Saturday" },
                    new SelectListItem { Value = "Sunday", Text = "Sunday", Selected = dayOfWeek == "Sunday" },
                    new SelectListItem { Value = "Monday", Text = "Monday", Selected = dayOfWeek == "Monday" },
                    new SelectListItem { Value = "Tuesday", Text = "Tuesday", Selected = dayOfWeek == "Tuesday" },
                    new SelectListItem { Value = "Wednesday", Text = "Wednesday", Selected = dayOfWeek == "Wednesday" },
                    new SelectListItem { Value = "Thursday", Text = "Thursday", Selected = dayOfWeek == "Thursday" },
                    new SelectListItem { Value = "Friday", Text = "Friday", Selected = dayOfWeek == "Friday" }
                },

                CurrentSort = currentSort,
                PatientSort = currentSort == "patient" ? "patient_desc" : "patient",
                DateSort = currentSort == "date" ? "date_desc" : "date",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Doctor/Views/History/GetAllAppointments.cshtml", vm);
        }

        [HttpGet("GetAppointment/{id:int}")]
        public async Task<IActionResult> GetAppointment(
            [FromRoute] int id, [FromQuery] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "Schedule.Clinic.Doctor.Department,Patient");
            if (appointment == null)
            {
                return NotFound();
            }

            string patientDisplayImageUrl;
            if (!string.IsNullOrEmpty(appointment.PatientImageUrl))
            {
                patientDisplayImageUrl = appointment.PatientImageUrl;
            }
            else
            {
                patientDisplayImageUrl = appointment.PatientGender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }

            vm.Appointment = appointment;
            vm.SourceView = "GetAppointment";

            vm.PatientDisplayImageUrl = patientDisplayImageUrl;
            vm.ClinicName = appointment.Schedule.Clinic.Name;
            vm.ClinicAddress = appointment.Schedule.Clinic.Address;
            vm.ClinicPhoneNumber = appointment.Schedule.Clinic.PhoneNumber;
            vm.ConsultationPrice = appointment.Schedule.Clinic.ConsultationPrice;
            vm.ScheduleDayOfWeek = appointment.Schedule.DayOfWeek;
            vm.AppointmentDuration = appointment.Schedule.AppointmentDuration;
            vm.ScheduleNotes = string.IsNullOrEmpty(appointment.Schedule.Notes) ? "No notes added" : appointment.Schedule.Notes;

            vm.IsPending = appointment.Status == SD.Status_Pending;
            vm.CanBeCompleted = appointment.Status == SD.Status_Confirmed && DateTime.Now >= appointment.AppointmentDate.Add(appointment.AppointmentTime);

            return View("Areas/Doctor/Views/History/GetAppointment.cshtml", vm);
        }
    }
}
