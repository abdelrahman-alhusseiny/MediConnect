using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Appointment;
using MedicalSystem.Models.DTOs.PatientAppointment;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("api/[area]/[controller]")]
    public class AppointmentApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AppointmentApiController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
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
                return Unauthorized();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound("Patient not found.");
            }

            int pageSize = 6;
            var (appointments, totalItems) =
                await _unitOfWork.Appointment.GetFilteredAppointmentsAsync(
                    patient.Id, null, searchString, null, dayOfWeek, currentSort, currentPage, pageSize,
                    new[] { SD.Status_Pending, SD.Status_Confirmed });

            var appointmentDtos = appointments.Select(a => new GetAllPatientAppointments
            {
                Id = a.Id,
                DepartmentName = a.Schedule.Clinic.Doctor.Department.Name,
                ClinicName = a.Schedule.Clinic.Name,
                ScheduleDayOfWeek = a.Schedule.DayOfWeek,
                AppointmentDate = a.AppointmentDate,
                AppointmentTime = a.AppointmentTime,
                Status = a.Status,
                IsPending = a.Status == SD.Status_Pending,
                IsPayable = a.Status == SD.Status_Confirmed
                        && a.PaymentMethod == SD.PaymentMethod_Online
                        && (a.Payment == null || a.Payment.Status != SD.PaymentStatus_Completed)
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
                PaymentMethod = appointment.PaymentMethod,

                IsPending = appointment.Status == SD.Status_Pending,
                IsPayable = appointment.Status == SD.Status_Confirmed
                    && appointment.PaymentMethod == SD.PaymentMethod_Online
                    && (appointment.Payment == null || appointment.Payment.Status != SD.PaymentStatus_Completed),
                PaymentStatus = appointment.Payment != null && appointment.Payment.Status == SD.PaymentStatus_Completed
                    ? SD.PaymentStatus_Completed
                    : SD.PaymentStatus_NotPaid
            };

            return Ok(dto);
        }

        [HttpPut("EditAppointment/{id:int}")]
        public async Task<IActionResult> EditAppointment([FromRoute] int id, [FromForm] EditPatientAppointmentDto dto)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "Schedule.Clinic.Doctor");
            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            if (appointment.Status != SD.Status_Pending)
            {
                return BadRequest(new { message = "Only pending appointments can be edited." });
            }

            if (dto.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only jpg, jpeg, png images are allowed");

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest("Image size must be less than 2MB");

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "appointments");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(appointment.PatientImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, appointment.PatientImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(fileStream);
                }

                appointment.PatientImageUrl = "/images/appointments/" + fileName + extension;
            }

            appointment.PatientName = dto.PatientName;
            appointment.PatientPhoneNumber = dto.PatientPhoneNumber;
            appointment.PatientAddress = dto.PatientAddress;
            appointment.PatientGender = dto.PatientGender;
            appointment.PatientAge = dto.PatientAge;
            appointment.ReasonForVisit = dto.ReasonForVisit;
            appointment.PaymentMethod = dto.PaymentMethod;

            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Updated",
                Message = $"Your appointment on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} has been updated successfully.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                Success = true,
                Message = "Appointment updated successfully.",
                AppointmentId = appointment.Id
            });
        }

        [HttpDelete("DeletePatientImage/{id:int}")]
        public async Task<IActionResult> DeletePatientImage([FromRoute] int id)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(a => a.Id == id);
            if (appointment == null)
            {
                return NotFound("Appointment not found.");
            }

            if (appointment.Status != SD.Status_Pending)
            {
                return BadRequest(new { message = "Only pending appointments can be edited." });
            }

            if (!string.IsNullOrEmpty(appointment.PatientImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, appointment.PatientImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                appointment.PatientImageUrl = null;
                await _unitOfWork.Appointment.UpdateAsync(appointment);
                await _unitOfWork.SaveAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Image deleted successfully.",
                    AppointmentId = appointment.Id
                });
            }

            return BadRequest(new
            {
                Success = false,
                Message = "No image found to delete.",
                AppointmentId = appointment.Id
            });
        }

        [HttpPost("CancelAppointment/{id:int}")]
        public async Task<IActionResult> CancelAppointment([FromRoute] int id)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Appointment not found."
                });
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
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} has been cancelled.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                Success = true,
                Message = "Appointment cancelled successfully.",
                AppointmentId = appointment.Id
            });
        }

        [HttpPost("PayAppointmentByCard/{appointmentId:int}")]
        public async Task<ActionResult> PayAppointmentByCard([FromRoute] int appointmentId, [FromForm] PayPalPaymentDto dto)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == appointmentId,
                includeProperties: "Schedule.Clinic.Doctor,Patient");

            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found." });
            }

            if (appointment.Status != SD.Status_Confirmed)
            {
                return BadRequest(new { message = "Only confirmed appointments can be paid." });
            }

            var existingPayment = await _unitOfWork.Payment.GetFirstOrDefaultAsync(
                p => p.AppointmentId == appointment.Id && p.Status == SD.PaymentStatus_Completed);

            if (existingPayment != null)
            {
                return BadRequest(new { message = "This appointment has already been paid. You cannot pay again." });
            }

            var result = await _unitOfWork.Payment.ProcessCardPaymentAsync(
                appointment.Id,
                appointment.Schedule.Clinic.ConsultationPrice,
                dto.CardNumber,
                dto.ExpiryMonth,
                dto.ExpiryYear,
                dto.CVV
            );

            var payment = new Payment
            {
                AppointmentId = appointment.Id,
                Amount = appointment.Schedule.Clinic.ConsultationPrice,
                Method = SD.PaymentMethod_Card,
                TransactionId = result.TransactionId,
                Status = result.Success ? SD.PaymentStatus_Completed : SD.PaymentStatus_Failed,
                PaidAt = result.Success ? DateTime.UtcNow : (DateTime?)null
            };

            await _unitOfWork.Payment.AddAsync(payment);
            appointment.Status = result.Success ? SD.Status_Confirmed : SD.Status_Pending;
            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var patientNotification = new Notification
            {
                Title = result.Success ? "Payment Successful" : "Payment Failed",
                Message = result.Success
                    ? $"Your payment for Dr. {appointment.Schedule.Clinic.Doctor.FullName} at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} was successful."
                    : $"Your payment attempt for Dr. {appointment.Schedule.Clinic.Doctor.FullName} at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} failed.",
                Type = SD.Type_Payment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId
            };
            await _unitOfWork.Notification.AddAsync(patientNotification);

            var doctorNotification = new Notification
            {
                Title = result.Success ? "Patient Payment Completed" : "Patient Payment Failed",
                Message = result.Success
                    ? $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, patient {appointment.Patient.FullName} has successfully paid for their appointment."
                    : $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, patient {appointment.Patient.FullName} attempted to pay but failed.",
                Type = SD.Type_Payment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            if (result.Success)
            {
                return Ok(new
                {
                    message = "Payment completed successfully.",
                    amount = payment.Amount,
                    appointmentId = payment.Id,
                    method = SD.PaymentMethod_Card,
                    transactionId = payment.TransactionId,
                    status = payment.Status,
                    paidAt = payment.PaidAt
                });
            }
            else
            {
                return BadRequest(new
                {
                    message = "Payment failed. Please try again.",
                    error = result.ErrorMessage,
                    amount = payment.Amount,
                    appointmentId = payment.Id,
                    method = SD.PaymentMethod_Card,
                    transactionId = payment.TransactionId,
                    status = payment.Status,
                    paidAt = payment.PaidAt
                });
            }
        }
    }
}
