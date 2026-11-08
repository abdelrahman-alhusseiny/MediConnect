using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Appointment;
using MedicalSystem.Models.Models;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("api/[area]/[controller]")]
    public class HistoryApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public HistoryApiController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
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
                return Unauthorized(new
                {
                    Success = false,
                    Message = "User not found."
                });
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Patient not found."
                });
            }

            int pageSize = 6;
            var (appointments, totalItems) =
                await _unitOfWork.Appointment.GetFilteredAppointmentsAsync(
                    patient.Id, null, searchString, null, dayOfWeek, currentSort, currentPage, pageSize,
                    new[] { SD.Status_Cancelled, SD.Status_Completed });

            var appointmentDtos = appointments.Select(a => new GetAllPatientAppointments
            {
                Id = a.Id,
                DepartmentName = a.Schedule.Clinic.Doctor.Department.Name,
                ClinicName = a.Schedule.Clinic.Name,
                ScheduleDayOfWeek = a.Schedule.DayOfWeek,
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime,
                Status = a.Status
            }).ToList();

            var response = new
            {
                Success = true,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Appointments = appointmentDtos
            };

            return Ok(response);
        }

        [HttpGet("GetAppointment/{id:int}")]
        public async Task<IActionResult> GetAppointment([FromRoute] int id)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id,
                includeProperties: "Schedule.Clinic.Doctor.Department,Patient");

            if (appointment == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Appointment not found."
                });
            }

            string doctorDisplayImageUrl = !string.IsNullOrEmpty(appointment.Schedule.Clinic.Doctor.ImageUrl)
                ? appointment.Schedule.Clinic.Doctor.ImageUrl
                : appointment.Schedule.Clinic.Doctor.Gender?.ToLower() == "male"
                    ? "/images/doctors/default-doctor-male.png"
                    : "/images/doctors/default-doctor-female.png";

            int doctorAge = DateTime.Now.Year - appointment.Schedule.Clinic.Doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < appointment.Schedule.Clinic.Doctor.DateOfBirth.AddYears(doctorAge).Date)
            {
                doctorAge--;
            }

            string patientDisplayImageUrl = !string.IsNullOrEmpty(appointment.PatientImageUrl)
                ? appointment.PatientImageUrl
                : appointment.PatientGender?.ToLower() == "male"
                    ? "/images/patients/default-patient-male.png"
                    : "/images/patients/default-patient-female.png";

            var dto = new GetPatientAppointment
            {
                Id = appointment.Id,

                DoctorDisplayImageUrl = doctorDisplayImageUrl,
                DoctorName = appointment.Schedule.Clinic.Doctor.FullName,
                DoctorAge = doctorAge,
                DepartmentName = appointment.Schedule.Clinic.Doctor.Department.Name,
                DoctorAboutMe = appointment.Schedule.Clinic.Doctor.AboutMe,

                ClinicName = appointment.Schedule.Clinic.Name,
                ClinicAddress = appointment.Schedule.Clinic.Address,
                ClinicPhoneNumber = appointment.Schedule.Clinic.PhoneNumber,
                ConsultationPrice = appointment.Schedule.Clinic.ConsultationPrice,

                ScheduleDayOfWeek = appointment.Schedule.DayOfWeek,
                AppointmentDate = appointment.AppointmentDate,
                AppointmentTime = appointment.AppointmentTime,
                AppointmentDuration = appointment.Schedule.AppointmentDuration,
                ScheduleNotes = string.IsNullOrEmpty(appointment.Schedule.Notes) ? "No notes added" : appointment.Schedule.Notes,
                Status = appointment.Status,
                CreatedAt = appointment.CreatedAt,

                PatientDisplayImageUrl = patientDisplayImageUrl,
                PatientName = appointment.PatientName,
                PatientPhoneNumber = appointment.PatientPhoneNumber,
                PatientAddress = appointment.PatientAddress,
                PatientGender = appointment.PatientGender,
                PatientAge = appointment.PatientAge,

                ReasonForVisit = appointment.ReasonForVisit,
                PaymentMethod = appointment.PaymentMethod
            };

            return Ok(new
            {
                Success = true,
                Appointment = dto
            });
        }
    }
}
