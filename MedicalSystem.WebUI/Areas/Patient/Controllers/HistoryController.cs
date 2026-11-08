using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("Patient/History")]
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
            [FromQuery] string searchString = "",
            [FromQuery] string dayOfWeek = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound();
            }

            int pageSize = 6;
            var (appointments, totalItems) =
                await _unitOfWork.Appointment.GetFilteredAppointmentsAsync(
                    patient.Id, null, searchString, null, dayOfWeek, currentSort, currentPage, pageSize,
                    new[] { SD.Status_Cancelled, SD.Status_Completed });

            var appointmentRows = new List<AppointmentRowVM>();
            foreach (var a in appointments)
            {
                appointmentRows.Add(new AppointmentRowVM
                {
                    Appointment = a,
                    ClinicName = a.Schedule.Clinic.Name,
                    AppointmentDay = a.Schedule.DayOfWeek,
                    DepartmentName = a.Schedule.Clinic.Doctor.Department.Name
                });
            }

            var vm = new AppointmentListVM
            {
                Appointments = appointmentRows,
                SourceView = "GetAllAppointments",

                SearchString = searchString,

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
                NameSort = currentSort == "name" ? "name_desc" : "name",
                DateSort = currentSort == "date" ? "date_desc" : "date",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Patient/Views/History/GetAllAppointments.cshtml", vm);
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

            string displayImageUrl;
            if (!string.IsNullOrEmpty(appointment.Schedule.Clinic.Doctor.ImageUrl))
            {
                displayImageUrl = appointment.Schedule.Clinic.Doctor.ImageUrl;
            }
            else
            {
                displayImageUrl = appointment.Schedule.Clinic.Doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }
            int age = DateTime.Now.Year - appointment.Schedule.Clinic.Doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < appointment.Schedule.Clinic.Doctor.DateOfBirth.AddYears(age).Date)
            {
                age--;
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

            vm.DoctorDisplayImageUrl = displayImageUrl;
            vm.DoctorName = appointment.Schedule.Clinic.Doctor.FullName;
            vm.DoctorAge = age;
            vm.DepartmentName = appointment.Schedule.Clinic.Doctor.Department.Name;
            vm.DoctorAboutMe = appointment.Schedule.Clinic.Doctor.AboutMe;
            vm.ClinicName = appointment.Schedule.Clinic.Name;
            vm.ClinicAddress = appointment.Schedule.Clinic.Address;
            vm.ClinicPhoneNumber = appointment.Schedule.Clinic.PhoneNumber;
            vm.ConsultationPrice = appointment.Schedule.Clinic.ConsultationPrice;
            vm.ScheduleDayOfWeek = appointment.Schedule.DayOfWeek;
            vm.AppointmentDuration = appointment.Schedule.AppointmentDuration;
            vm.ScheduleNotes = string.IsNullOrEmpty(appointment.Schedule.Notes) ? "No notes added" : appointment.Schedule.Notes;
            vm.PatientDisplayImageUrl = patientDisplayImageUrl;

            return View("Areas/Patient/Views/History/GetAppointment.cshtml", vm);
        }
    }
}
