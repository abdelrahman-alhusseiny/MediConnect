using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Password;
using MedicalSystem.Models.DTOs.Patient;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;

namespace MedicalSystem.WebUI.Areas.Admin.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Authorize(Roles = SD.Role_Admin)]
    [Area("Admin")]
    [Route("api/[area]/[controller]")]
    public class PatientApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PatientApiController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllPatients")]
        public async Task<IActionResult> GetAllPatients(
        [FromQuery] string searchString = "",
        [FromQuery] string lockoutStatus = "",
        [FromQuery] string currentSort = "",
        [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;

            var (patients, totalItems) =
                await _unitOfWork.Patient.GetFilteredPatientsAsync(
                    searchString, lockoutStatus, currentSort, currentPage, pageSize);

            var dtoList = new List<GetAllPatientsDto>();

            foreach (var p in patients)
            {
                string displayImageUrl = !string.IsNullOrEmpty(p.ImageUrl)
                    ? p.ImageUrl
                    : p.Gender?.ToLower() == "male"
                        ? "/images/patients/default-patient-male.png"
                        : "/images/patients/default-patient-female.png";

                var appUserLockoutStatus = await _userManager.IsLockedOutAsync(p.AppUser) ? "Locked" : "Unlocked";

                dtoList.Add(new GetAllPatientsDto
                {
                    Id = p.Id,
                    DisplayImageUrl = displayImageUrl,
                    FullName = p.FullName,
                    AppUserEmail = p.AppUser.Email,
                    AppUserLockoutStatus = appUserLockoutStatus,
                    CreatedAt = p.CreatedAt
                });
            }

            var response = new
            {
                Patients = dtoList,
                SearchString = searchString,
                LockoutStatus = lockoutStatus,
                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(response);
        }

        [HttpGet("GetPatient/{id:int}")]
        public async Task<IActionResult> GetPatient([FromRoute] int id)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeProperties: "AppUser"
            );

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            string displayImageUrl = !string.IsNullOrEmpty(patient.ImageUrl)
                ? patient.ImageUrl
                : patient.Gender?.ToLower() == "male"
                    ? "/images/patients/default-patient-male.png"
                    : "/images/patients/default-patient-female.png";

            var appUserLockoutStatus = await _userManager.IsLockedOutAsync(patient.AppUser) ? "Locked" : "Unlocked";
            var isLockedOut = await _userManager.IsLockedOutAsync(patient.AppUser);

            var dto = new GetPatientDto
            {
                Id = patient.Id,
                DisplayImageUrl = displayImageUrl,
                FullName = patient.FullName,
                AppUserEmail = patient.AppUser.Email,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                AppUserLockoutStatus = appUserLockoutStatus,
                CreatedAt = patient.CreatedAt
            };

            return Ok(dto);
        }

        [HttpPost("RegisterPatient")]
        public async Task<IActionResult> RegisterPatient([FromForm] RegisterPatientDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
            {
                return BadRequest(new { message = "The password and confirmation password do not match." });
            }

            if (dto.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Only jpg, jpeg, png images are allowed" });

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "Image size must be less than 2MB" });
            }

            var appUser = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(appUser, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            await _userManager.AddToRoleAsync(appUser, SD.Role_Patient);

            string? imageUrl = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "patients");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(dto.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = "/images/patients/" + fileName + extension;
            }

            var patient = new MedicalSystem.Models.Models.Patient
            {
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                ImageUrl = imageUrl,
                AppUserId = appUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Patient.AddAsync(patient);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Account Created",
                Message = $"Welcome {patient.FullName}, your patient account has been created successfully.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Patient registered successfully.",
            });
        }

        [HttpPut("EditPatient/{id:int}")]
        public async Task<IActionResult> EditPatient([FromRoute] int id, [FromForm] EditPatientDto dto)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.Id == id);
            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            if (dto.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest(new { message = "Only jpg, jpeg, png images are allowed" });

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "Image size must be less than 2MB" });
            }

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "patients");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(patient.ImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, patient.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(dto.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await dto.ImageFile.CopyToAsync(fileStream);
                }

                patient.ImageUrl = "/images/patients/" + fileName + extension;
            }

            patient.FullName = dto.FullName;
            patient.PhoneNumber = dto.PhoneNumber;
            patient.Address = dto.Address;
            patient.Gender = dto.Gender;
            patient.DateOfBirth = dto.DateOfBirth;

            await _unitOfWork.Patient.UpdateAsync(patient);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Account Updated",
                Message = $"Patient account for {patient.FullName} has been updated successfully.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Patient updated successfully." });
        }

        [HttpDelete("DeletePatientImage/{id:int}")]
        public async Task<IActionResult> DeletePatientImage([FromRoute] int id)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.Id == id);
            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, patient.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                patient.ImageUrl = null;
                await _unitOfWork.Patient.UpdateAsync(patient);
                await _unitOfWork.SaveAsync();

                return Ok(new { message = "Image deleted successfully." });
            }

            return BadRequest(new { message = "Patient has no image to delete." });
        }

        [HttpDelete("DeletePatient/{id:int}")]
        public async Task<IActionResult> DeletePatient([FromRoute] int id)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeProperties: "AppUser,Appointments.Schedule.Clinic.Doctor"
            );

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, patient.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            var notifications = await _unitOfWork.Notification
                .GetAllAsync(n => n.PatientId == patient.Id);

            if (notifications.Any())
            {
                _unitOfWork.Notification.RemoveRange(notifications);
            }

            var reviews = await _unitOfWork.DoctorReview
               .GetAllAsync(r => r.PatientId == patient.Id);

            if (reviews.Any())
            {
                _unitOfWork.DoctorReview.RemoveRange(reviews);
            }

            if (patient.Appointments != null && patient.Appointments.Any())
            {
                foreach (var appointment in patient.Appointments)
                {
                    var doctor = appointment.Schedule?.Clinic?.Doctor;
                    if (doctor != null)
                    {
                        var doctorNotification = new Notification
                        {
                            Title = "Patient Account Deleted",
                            Message = $"Dear Dr. {doctor.FullName}, the patient {patient.FullName} " +
                                      $"who had an appointment with you at {appointment.Schedule.Clinic.Name} " +
                                      $"on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} " +
                                      $"has had their account deleted by the admin.",
                            Type = SD.Type_Account,
                            Status = SD.Status_Unreaded,
                            DoctorId = doctor.Id,
                        };

                        await _unitOfWork.Notification.AddAsync(doctorNotification);
                    }
                }

                _unitOfWork.Appointment.RemoveRange(patient.Appointments);
            }

            _unitOfWork.Patient.Remove(patient);
            await _userManager.DeleteAsync(patient.AppUser);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Patient account has been deleted successfully." });
        }

        [HttpPost("TogglePatientLock/{id:int}")]
        public async Task<IActionResult> TogglePatientLock([FromRoute] int id)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeProperties: "AppUser"
            );

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            if (await _userManager.IsLockedOutAsync(patient.AppUser))
            {
                await _userManager.SetLockoutEndDateAsync(patient.AppUser, null);
                await _userManager.ResetAccessFailedCountAsync(patient.AppUser);

                var notification = new Notification
                {
                    Title = "Account Unlocked",
                    Message = $"Your patient account has been unlocked. You can now log in again.",
                    Type = SD.Type_Account,
                    Status = SD.Status_Unreaded,
                    PatientId = patient.Id,
                };

                await _unitOfWork.Notification.AddAsync(notification);
                await _unitOfWork.SaveAsync();
                return Ok(new { message = "Patient has been unlocked successfully." });
            }
            else
            {
                var lockoutDuration = TimeSpan.FromDays(365 * 10);
                await _userManager.SetLockoutEndDateAsync(patient.AppUser, DateTimeOffset.UtcNow.Add(lockoutDuration));

                var notification = new Notification
                {
                    Title = "Account Locked",
                    Message = $"Your patient account has been locked by the administrator. Please contact support for assistance.",
                    Type = SD.Type_Account,
                    Status = SD.Status_Unreaded,
                    PatientId = patient.Id,
                };

                await _unitOfWork.Notification.AddAsync(notification);
                await _unitOfWork.SaveAsync();
                return Ok(new { message = "Patient has been locked successfully." });
            }
        }

        [HttpPost("ResetPatientPassword/{id:int}")]
        public async Task<IActionResult> ResetPatientPassword([FromRoute] int id, [FromForm] ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest(new { message = "The new password and confirmation password do not match." });
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "AppUser");

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(patient.AppUser);
            var result = await _userManager.ResetPasswordAsync(patient.AppUser, token, dto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Password reset failed.",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            await _userManager.UpdateSecurityStampAsync(patient.AppUser);

            var notification = new Notification
            {
                Title = "Password Reset",
                Message = $"Your patient account password has been reset successfully by the administrator.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id,
            };

            return Ok(new
            {
                message = "Password reset successfully.",
                email = patient.AppUser.Email
            });
        }
    }
}
