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
    public class AppointmentApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentApiController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
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
                    new[] { SD.Status_Pending, SD.Status_Confirmed });

            var appointmentDtos = appointments.Select(a => new GetAllDoctorAppointmentsDto
            {
                Id = a.Id,
                ClinicName = a.Schedule.Clinic.Name,
                PatientName = a.PatientName,
                ScheduleDayOfWeek = a.Schedule.DayOfWeek,
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime,
                Status = a.Status,
                IsPending = a.Status == SD.Status_Pending,
                CanBeCompleted = a.Status == SD.Status_Confirmed && DateTime.Now >= a.AppointmentDate.Add(a.AppointmentTime)
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
                PaymentMethod = appointment.PaymentMethod,

                IsPending = appointment.Status == SD.Status_Pending,
                CanBeCompleted = appointment.Status == SD.Status_Confirmed && DateTime.Now >= appointment.AppointmentDate.Add(appointment.AppointmentTime),
                PaymentStatus = appointment.Payment != null && appointment.Payment.Status == SD.PaymentStatus_Completed
                    ? SD.PaymentStatus_Completed
                    : SD.PaymentStatus_NotPaid
            };

            return Ok(dto);
        }

        [HttpPost("CancelAppointment/{id:int}")]
        public async Task<IActionResult> CancelAppointment([FromRoute] int id)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id,
                includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            if (appointment.Status != SD.Status_Pending)
            {
                return BadRequest(new { message = "Only pending appointments can be cancelled." });
            }

            appointment.Status = SD.Status_Cancelled;
            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Cancelled",
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
                          $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                          $"at {appointment.AppointmentTime} has been cancelled by the doctor.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "Appointment Cancelled",
                Message = $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, you cancelled the appointment " +
                  $"with patient ID {appointment.PatientId} at clinic \"{appointment.Schedule.Clinic.Name}\" " +
                  $"scheduled for {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            var response = new
            {
                Success = true,
                Message = "Appointment cancelled successfully.",
                AppointmentId = appointment.Id,
                Status = appointment.Status
            };

            return Ok(response);
        }

        [HttpPost("ConfirmAppointment/{id:int}")]
        public async Task<IActionResult> ConfirmAppointmentApi([FromRoute] int id)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id,
                includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            if (appointment.Status != SD.Status_Pending)
            {
                return BadRequest(new { message = "Only pending appointments can be confirmed." });
            }

            appointment.Status = SD.Status_Confirmed;
            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Confirmed",
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
                  $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointment.AppointmentTime} has been confirmed.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "Appointment Confirmed",
                Message = $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, you confirmed the appointment " +
                  $"with patient ID {appointment.PatientId} at clinic \"{appointment.Schedule.Clinic.Name}\" " +
                  $"scheduled for {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            if (appointment.PaymentMethod == SD.PaymentMethod_Online)
            {
                var paymentNotification = new Notification
                {
                    Title = "Payment Required",
                    Message = "Please complete your online payment to secure your appointment.",
                    Type = SD.Type_Payment,
                    Status = SD.Status_Unreaded,
                    PatientId = appointment.PatientId
                };
                await _unitOfWork.Notification.AddAsync(paymentNotification);
            }
            else if (appointment.PaymentMethod == SD.PaymentMethod_Cash)
            {
                var paymentNotification = new Notification
                {
                    Title = "Pay at Clinic",
                    Message = "Please pay at the clinic reception before your consultation.",
                    Type = SD.Type_Payment,
                    Status = SD.Status_Unreaded,
                    PatientId = appointment.PatientId
                };
                await _unitOfWork.Notification.AddAsync(paymentNotification);
            }

            await _unitOfWork.SaveAsync();

            var response = new
            {
                Success = true,
                Message = "Appointment confirmed successfully.",
                AppointmentId = appointment.Id,
                Status = appointment.Status
            };

            return Ok(response);
        }

        [HttpPost("CompleteAppointment/{id:int}")]
        public async Task<IActionResult> CompleteAppointmentApi([FromRoute] int id)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id,
                includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            if (appointment.Status != SD.Status_Confirmed)
            {
                return BadRequest(new { message = "Only confirmed appointments can be completed." });
            }

            appointment.Status = SD.Status_Completed;
            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Completed",
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
                          $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                          $"at {appointment.AppointmentTime} has been marked as completed.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "Appointment Completed",
                Message = $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, you marked the appointment " +
                          $"with patient ID {appointment.PatientId} at clinic \"{appointment.Schedule.Clinic.Name}\" " +
                          $"on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} as completed.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            var response = new
            {
                Success = true,
                Message = "Appointment completed successfully.",
                AppointmentId = appointment.Id,
                Status = appointment.Status
            };

            return Ok(response);
        }
    }
}
