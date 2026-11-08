using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.DoctorAppointment;
using MedicalSystem.Models.Models;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Doctor.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Authorize(Roles = SD.Role_Doctor)]
    [Area("Doctor")]
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
            [FromQuery] int? clinicId = null,
            [FromQuery] string dayOfWeek = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return Unauthorized();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound("Doctor not found.");
            }

            int pageSize = 6;
            var (appointments, totalItems) =
                await _unitOfWork.Appointment.GetFilteredAppointmentsAsync(
                    null, doctor.Id, "", clinicId, dayOfWeek, currentSort, currentPage, pageSize,
                    new[] { SD.Status_Cancelled, SD.Status_Completed });

            var appointmentDtos = appointments.Select(a => new GetAllDoctorAppointmentsDto
            {
                Id = a.Id,
                ClinicName = a.Schedule.Clinic.Name,
                PatientName = a.PatientName,
                ScheduleDayOfWeek = a.Schedule.DayOfWeek,
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime,
                Status = a.Status
            }).ToList();

            var response = new
            {
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
                return NotFound("Appointment not found.");
            }

            string patientDisplayImageUrl;
            if (!string.IsNullOrEmpty(appointment.PatientImageUrl))
            {
                patientDisplayImageUrl = appointment.PatientImageUrl;
            }
            else
            {
                patientDisplayImageUrl = appointment.PatientGender?.ToLower() == "male"
                    ? "/images/patients/default-patient-male.png"
                    : "/images/patients/default-patient-female.png";
            }

            var dto = new GetDoctorAppointmentDto
            {
                Id = appointment.Id,

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

            return Ok(dto);
        }
    }
}
