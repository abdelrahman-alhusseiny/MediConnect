using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Booking;
using MedicalSystem.Models.DTOs.PatientBooking;
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
    public class BookingApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BookingApiController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllDoctors")]
        public async Task<IActionResult> GetAllDoctors(
            [FromQuery] string searchString = "",
            [FromQuery] int? departmentId = null,
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (doctors, totalItems) =
                await _unitOfWork.Doctor.GetFilteredDoctorsAsync(
                    searchString, "", departmentId, currentSort, currentPage, pageSize);

            var doctorDtos = new List<GetAllDoctorsDto>();
            foreach (var d in doctors)
            {
                string displayImageUrl;
                if (!string.IsNullOrEmpty(d.ImageUrl))
                {
                    displayImageUrl = d.ImageUrl;
                }
                else
                {
                    displayImageUrl = d.Gender?.ToLower() == "male"
                        ? "/images/doctors/default-doctor-male.png"
                        : "/images/doctors/default-doctor-female.png";
                }

                int age = DateTime.Now.Year - d.DateOfBirth.Year;
                if (DateTime.Now.Date < d.DateOfBirth.AddYears(age).Date)
                {
                    age--;
                }

                doctorDtos.Add(new GetAllDoctorsDto
                {
                    DoctorId = d.Id,
                    DisplayImageUrl = displayImageUrl,
                    FullName = d.FullName,
                    DepartmentName = d.Department.Name,
                    Gender = d.Gender,
                    Age = age,
                    AverageRating = await _unitOfWork.DoctorReview.GetAverageRatingAsync(d.Id)
                });
            }

            return Ok(new
            {
                Doctors = doctorDtos,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            });
        }

        [HttpGet("GetDoctor/{doctorId:int}")]
        public async Task<IActionResult> GetDoctor([FromRoute] int doctorId)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == doctorId, includeProperties: "AppUser,Department");

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            string displayImageUrl = !string.IsNullOrEmpty(doctor.ImageUrl)
                ? doctor.ImageUrl
                : doctor.Gender?.ToLower() == "male"
                    ? "/images/doctors/default-doctor-male.png"
                    : "/images/doctors/default-doctor-female.png";

            int age = DateTime.Now.Year - doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < doctor.DateOfBirth.AddYears(age).Date)
            {
                age--;
            }

            var dto = new GetDoctorDto
            {
                DoctorId = doctor.Id,
                DisplayImageUrl = displayImageUrl,
                FullName = doctor.FullName,
                AppUserEmail = doctor.AppUser.Email,
                PhoneNumber = doctor.PhoneNumber,
                Gender = doctor.Gender,
                Age = age,
                DepartmentName = doctor.Department.Name,
                DepartmentDescription = doctor.Department.Description,
                AboutMe = doctor.AboutMe,
                AverageRating = await _unitOfWork.DoctorReview.GetAverageRatingAsync(doctor.Id)
            };

            return Ok(dto);
        }

        [HttpGet("GetDoctorReviews/{doctorId:int}")]
        public async Task<ActionResult<IEnumerable<GetDoctorReviewsDto>>> GetDoctorReviews([FromRoute] int doctorId)
        {
            var doctorReviews = await _unitOfWork.DoctorReview.GetAllAsync(
                r => r.DoctorId == doctorId,
                includeProperties: "Patient");

            if (doctorReviews == null || !doctorReviews.Any())
            {
                return NotFound("No reviews found for this doctor.");
            }

            var reviewDtos = doctorReviews.Select(r => new GetDoctorReviewsDto
            {
                PatientImageUrl = !string.IsNullOrEmpty(r.Patient.ImageUrl)
                    ? r.Patient.ImageUrl
                    : (r.Patient.Gender?.ToLower() == "male"
                        ? "/images/patients/default-patient-male.png"
                        : "/images/patients/default-patient-female.png"),
                PatientName = r.Patient.FullName,
                Rating = r.Rating,
                Comment = r.Comment ?? string.Empty,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Ok(reviewDtos);
        }

        [HttpPost("CreateDoctorReview/{doctorId:int}")]
        public async Task<ActionResult> CreateDoctorReview([FromRoute] int doctorId, [FromBody] DoctorReviewDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return Unauthorized("User not found or not logged in.");
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound("Patient record not found.");
            }

            var existingReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(
                r => r.DoctorId == doctorId && r.PatientId == patient.Id);

            if (existingReview != null)
            {
                return BadRequest("You have already submitted a review for this doctor.");
            }

            var doctorReview = new DoctorReview
            {
                DoctorId = doctorId,
                PatientId = patient.Id,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DoctorReview.AddAsync(doctorReview);
            await _unitOfWork.SaveAsync();

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.Id == doctorId);
            if (doctor == null)
            {
                return NotFound("Doctor not found.");
            }

            var patientNotification = new Notification
            {
                Title = "Review Submitted",
                Message = $"Thank you! Your review for Dr. {doctor.FullName} has been submitted successfully.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };
            await _unitOfWork.Notification.AddAsync(patientNotification);

            var doctorNotification = new Notification
            {
                Title = "New Review Received",
                Message = $"Dear Dr. {doctor.FullName}, you have received a new review from patient {patient.FullName}. " +
                          $"Rating: {dto.Rating} stars. Comment: \"{dto.Comment}\"",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                DoctorId = doctorId
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            return CreatedAtAction(nameof(GetDoctorReviews), new { doctorId }, new
            {
                doctorReview.Id,
                doctorReview.DoctorId,
                doctorReview.PatientId,
                doctorReview.Rating,
                doctorReview.Comment,
                doctorReview.CreatedAt
            });
        }

        [HttpPut("EditDoctorReview/{doctorId:int}")]
        public async Task<ActionResult> EditDoctorReview([FromRoute] int doctorId, [FromBody] DoctorReviewDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return Unauthorized("User not found or not logged in.");
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound("Patient record not found.");
            }

            var existingReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(
                r => r.DoctorId == doctorId && r.PatientId == patient.Id,
                includeProperties: "Doctor");

            if (existingReview == null)
            {
                return NotFound("You have not submitted a review for this doctor.");
            }

            existingReview.Rating = dto.Rating;
            existingReview.Comment = dto.Comment;

            await _unitOfWork.DoctorReview.UpdateAsync(existingReview);
            await _unitOfWork.SaveAsync();

            var doctor = existingReview.Doctor;
            if (doctor == null)
            {
                return NotFound("Doctor not found.");
            }

            var patientNotification = new Notification
            {
                Title = "Review Updated",
                Message = $"Your review for Dr. {doctor.FullName} has been updated successfully.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };
            await _unitOfWork.Notification.AddAsync(patientNotification);

            var doctorNotification = new Notification
            {
                Title = "Review Updated",
                Message = $"Dear Dr. {doctor.FullName}, patient {patient.FullName} has updated their review. " +
                          $"New Rating: {dto.Rating} stars. Comment: \"{dto.Comment}\"",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                DoctorId = doctorId
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                existingReview.Id,
                existingReview.DoctorId,
                existingReview.PatientId,
                existingReview.Rating,
                existingReview.Comment,
                existingReview.CreatedAt
            });
        }

        [HttpDelete("DeleteDoctorReview/{doctorId:int}")]
        public async Task<ActionResult> DeleteDoctorReview([FromRoute] int doctorId)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return Unauthorized("User not found or not logged in.");
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound("Patient record not found.");
            }

            var doctorReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(
                r => r.DoctorId == doctorId && r.PatientId == patient.Id,
                includeProperties: "Doctor");

            if (doctorReview == null)
            {
                return NotFound("You have not submitted a review for this doctor.");
            }

            if (doctorReview.Doctor == null)
            {
                return NotFound("Doctor record not found.");
            }

            _unitOfWork.DoctorReview.Remove(doctorReview);
            await _unitOfWork.SaveAsync();

            var patientNotification = new Notification
            {
                Title = "Review Deleted",
                Message = $"Your review for Dr. {doctorReview.Doctor.FullName} has been deleted successfully.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };
            await _unitOfWork.Notification.AddAsync(patientNotification);

            var doctorNotification = new Notification
            {
                Title = "Review Deleted",
                Message = $"Dear Dr. {doctorReview.Doctor.FullName}, patient {patient.FullName} deleted their review on {DateTime.UtcNow:dd MMM yyyy HH:mm}.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                DoctorId = doctorId
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Doctor review deleted successfully.",
                doctorReview.Id,
                doctorReview.DoctorId,
                doctorReview.PatientId
            });
        }

        [HttpGet("GetDoctorClinics/{doctorId:int}")]
        public async Task<IActionResult> GetDoctorClinics(
            [FromRoute] int doctorId,
            [FromQuery] string searchString = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (clinics, totalItems) =
                await _unitOfWork.Clinic.GetFilteredClinicsAsync(
                    doctorId, searchString, currentSort, currentPage, pageSize);

            var clinicDtos = new List<GetDoctorClinicsDto>();
            foreach (var c in clinics)
            {
                clinicDtos.Add(new GetDoctorClinicsDto
                {
                    ClinicId = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    PhoneNumber = c.PhoneNumber,
                    ConsultationPrice = c.ConsultationPrice
                });
            }

            return Ok(new
            {
                DoctorId = doctorId,
                Clinics = clinicDtos,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            });
        }

        [HttpGet("GetClinicSchedules/{clinicId:int}")]
        public async Task<IActionResult> GetClinicSchedules(
            [FromRoute] int clinicId,
            [FromQuery] string dayOfWeek = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (schedules, totalItems) =
                await _unitOfWork.Schedule.GetFilteredSchedulesAsync(
                    null, clinicId, dayOfWeek, currentSort, currentPage, pageSize);

            var scheduleDtos = new List<GetClinicSchedulesDto>();
            foreach (var s in schedules)
            {
                scheduleDtos.Add(new GetClinicSchedulesDto
                {
                    ScheduleId = s.Id,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    AppointmentDuration = s.AppointmentDuration,
                    Notes = string.IsNullOrEmpty(s.Notes) ? "No notes added" : s.Notes
                });
            }

            return Ok(new
            {
                ClinicId = clinicId,
                Schedules = scheduleDtos,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            });
        }

        [HttpGet("AppointmentSummary/{scheduleId:int}")]
        public async Task<IActionResult> AppointmentSummary([FromRoute] int scheduleId)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == scheduleId, includeProperties: "Clinic.Doctor.Department");
            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }

            string doctorDisplayImageUrl = !string.IsNullOrEmpty(schedule.Clinic.Doctor.ImageUrl)
                ? schedule.Clinic.Doctor.ImageUrl
                : schedule.Clinic.Doctor.Gender?.ToLower() == "male"
                    ? "/images/doctors/default-doctor-male.png"
                    : "/images/doctors/default-doctor-female.png";

            int doctorAge = DateTime.Now.Year - schedule.Clinic.Doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < schedule.Clinic.Doctor.DateOfBirth.AddYears(doctorAge).Date)
                doctorAge--;

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return NotFound(new { message = "User not found." });

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            string patientDisplayImageUrl = !string.IsNullOrEmpty(patient.ImageUrl)
                ? patient.ImageUrl
                : patient.Gender?.ToLower() == "male"
                    ? "/images/patients/default-patient-male.png"
                    : "/images/patients/default-patient-female.png";

            int patientAge = DateTime.Now.Year - patient.DateOfBirth.Year;
            if (DateTime.Now.Date < patient.DateOfBirth.AddYears(patientAge).Date)
                patientAge--;

            var dto = new AppointmentSummaryDto
            {
                ScheduleId = scheduleId,

                DoctorDisplayImageUrl = doctorDisplayImageUrl,
                DoctorName = schedule.Clinic.Doctor.FullName,
                DoctorAge = doctorAge,
                DepartmentName = schedule.Clinic.Doctor.Department.Name,
                DoctorAboutMe = schedule.Clinic.Doctor.AboutMe,

                ClinicName = schedule.Clinic.Name,
                ClinicAddress = schedule.Clinic.Address,
                ClinicPhoneNumber = schedule.Clinic.PhoneNumber,
                ConsultationPrice = schedule.Clinic.ConsultationPrice,

                ScheduleDayOfWeek = schedule.DayOfWeek,
                ScheduleStartTime = schedule.StartTime,
                ScheduleEndTime = schedule.EndTime,
                AppointmentDuration = schedule.AppointmentDuration,
                ScheduleNotes = string.IsNullOrEmpty(schedule.Notes) ? "No notes added" : schedule.Notes,

                PatientDisplayImageUrl = patientDisplayImageUrl,
                PatientName = patient.FullName,
                PatientPhoneNumber = patient.PhoneNumber,
                PatientAddress = patient.Address,
                PatientGender = patient.Gender,
                PatientAge = patientAge
            };

            return Ok(dto);
        }

        [HttpPost("BookAppointment/{scheduleId:int}")]
        public async Task<IActionResult> BookAppointment(
            [FromRoute] int scheduleId,
            [FromBody] BookAppointmentDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return Unauthorized(new { message = "User not found." });

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == scheduleId, includeProperties: "Clinic.Doctor.Department");
            if (schedule == null)
                return NotFound(new { message = "Schedule not found." });

            int patientAge = DateTime.Now.Year - patient.DateOfBirth.Year;
            if (DateTime.Now.Date < patient.DateOfBirth.AddYears(patientAge).Date)
                patientAge--;

            var existingPatientAppointments = await _unitOfWork.Appointment
               .GetAllAsync(a => a.ScheduleId == scheduleId && a.PatientId == patient.Id);

            DayOfWeek targetDay = schedule.DayOfWeek.ToLower() switch
            {
                "sunday" => DayOfWeek.Sunday,
                "monday" => DayOfWeek.Monday,
                "tuesday" => DayOfWeek.Tuesday,
                "wednesday" => DayOfWeek.Wednesday,
                "thursday" => DayOfWeek.Thursday,
                "friday" => DayOfWeek.Friday,
                "saturday" => DayOfWeek.Saturday,
                _ => throw new ArgumentException($"Invalid day of week: {schedule.DayOfWeek}")
            };

            var today = DateTime.Today;
            var now = DateTime.Now;

            int daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;

            if (daysUntilTarget == 0)
            {
                DateTime scheduleStart = today.Add(schedule.StartTime);

                if (now < scheduleStart)
                {
                    daysUntilTarget = 0;
                }
                else
                {
                    daysUntilTarget = 7;
                }
            }

            int weeksToAdd = existingPatientAppointments.Count();
            daysUntilTarget += (7 * weeksToAdd);

            DateTime appointmentDate = today.AddDays(daysUntilTarget);

            var existingAppointments = await _unitOfWork.Appointment
                    .GetAllAsync(a => a.ScheduleId == scheduleId
                   && a.AppointmentDate == appointmentDate
                   && a.Status != SD.Status_Cancelled);

            TimeSpan appointmentTime = schedule.StartTime;
            while (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) <= schedule.EndTime)
            {
                bool isTaken = existingAppointments.Any(a => a.AppointmentTime == appointmentTime);
                if (!isTaken)
                {
                    break;
                }
                appointmentTime = appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration));
            }

            if (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) > schedule.EndTime &&
                existingAppointments.Any(a => a.AppointmentTime == appointmentTime))
            {
                return BadRequest(new { message = "No available slots in this schedule." });
            }

            var appointment = new Appointment
            {
                PatientName = patient.FullName,
                PatientPhoneNumber = patient.PhoneNumber,
                PatientGender = patient.Gender,
                PatientAge = patientAge,
                PatientAddress = patient.Address,
                PatientImageUrl = patient.ImageUrl,
                ReasonForVisit = dto.ReasonForVisit,
                PaymentMethod = dto.PaymentMethod,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                Status = SD.Status_Pending,
                ScheduleId = schedule.Id,
                PatientId = patient.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Appointment.AddAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Booked",
                Message = $"Your appointment with Dr. {schedule.Clinic.Doctor.FullName} at {schedule.Clinic.Name} has been booked for {appointmentDate:dddd, dd MMM yyyy} at {appointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "New Appointment Booked",
                Message = $"Dear Dr. {schedule.Clinic.Doctor.FullName}, a new appointment has been booked " +
                  $"in your schedule at clinic \"{schedule.Clinic.Name}\" for {appointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointmentTime} by patient ID {patient.Id}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = schedule.Clinic.Doctor.Id
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Appointment booked successfully.",
                appointmentId = appointment.Id,
                appointmentDate = appointment.AppointmentDate,
                appointmentTime = appointment.AppointmentTime,
                status = appointment.Status
            });
        }

        [HttpPost("BookForOther/{scheduleId:int}")]
        public async Task<IActionResult> BookForOther(
            [FromRoute] int scheduleId,
            [FromForm] BookForOtherDto dto)
        {
            if (dto.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Only jpg, jpeg, png images are allowed." });

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "Image size must be less than 2MB." });
            }

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return Unauthorized(new { message = "User not found." });

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
                return NotFound(new { message = "Patient not found." });

            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == scheduleId, includeProperties: "Clinic.Doctor.Department");
            if (schedule == null)
                return NotFound(new { message = "Schedule not found." });

            var existingPatientAppointments = await _unitOfWork.Appointment
                .GetAllAsync(a => a.ScheduleId == scheduleId && a.PatientId == patient.Id);

            DayOfWeek targetDay = schedule.DayOfWeek.ToLower() switch
            {
                "sunday" => DayOfWeek.Sunday,
                "monday" => DayOfWeek.Monday,
                "tuesday" => DayOfWeek.Tuesday,
                "wednesday" => DayOfWeek.Wednesday,
                "thursday" => DayOfWeek.Thursday,
                "friday" => DayOfWeek.Friday,
                "saturday" => DayOfWeek.Saturday,
                _ => throw new ArgumentException($"Invalid day of week: {schedule.DayOfWeek}")
            };

            var today = DateTime.Today;
            var now = DateTime.Now;

            int daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;

            if (daysUntilTarget == 0)
            {
                DateTime scheduleStart = today.Add(schedule.StartTime);

                if (now < scheduleStart)
                {
                    daysUntilTarget = 0;
                }
                else
                {
                    daysUntilTarget = 7;
                }
            }

            int weeksToAdd = existingPatientAppointments.Count();
            daysUntilTarget += (7 * weeksToAdd);

            DateTime appointmentDate = today.AddDays(daysUntilTarget);

            var existingAppointments = await _unitOfWork.Appointment
                    .GetAllAsync(a => a.ScheduleId == scheduleId
                   && a.AppointmentDate == appointmentDate
                   && a.Status != SD.Status_Cancelled);

            TimeSpan appointmentTime = schedule.StartTime;
            while (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) <= schedule.EndTime)
            {
                bool isTaken = existingAppointments.Any(a => a.AppointmentTime == appointmentTime);
                if (!isTaken)
                {
                    break;
                }
                appointmentTime = appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration));
            }

            if (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) > schedule.EndTime &&
                existingAppointments.Any(a => a.AppointmentTime == appointmentTime))
            {
                return BadRequest(new { message = "No available slots in this schedule." });
            }

            string? patientImageUrl = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "appointments");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(dto.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(fileStream);
                }

                patientImageUrl = "/images/appointments/" + fileName + extension;
            }

            var appointment = new Appointment
            {
                PatientName = dto.PatientName,
                PatientPhoneNumber = dto.PatientPhoneNumber,
                PatientGender = dto.PatientGender,
                PatientAge = dto.PatientAge,
                PatientAddress = dto.PatientAddress,
                PatientImageUrl = patientImageUrl,
                ReasonForVisit = dto.ReasonForVisit,
                PaymentMethod = dto.PaymentMethod,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                Status = SD.Status_Pending,
                ScheduleId = schedule.Id,
                PatientId = patient.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Appointment.AddAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Booked",
                Message = $"Your appointment with Dr. {schedule.Clinic.Doctor.FullName} at {schedule.Clinic.Name} has been booked for {appointmentDate:dddd, dd MMM yyyy} at {appointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "New Appointment Booked",
                Message = $"Dear Dr. {schedule.Clinic.Doctor.FullName}, a new appointment has been booked " +
                  $"in your schedule at clinic \"{schedule.Clinic.Name}\" for {appointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointmentTime} by patient ID {patient.Id}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = schedule.Clinic.Doctor.Id
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Appointment booked successfully for another person.",
                appointmentId = appointment.Id,
                appointmentDate = appointment.AppointmentDate,
                appointmentTime = appointment.AppointmentTime,
                status = appointment.Status
            });
        }
    }
}
