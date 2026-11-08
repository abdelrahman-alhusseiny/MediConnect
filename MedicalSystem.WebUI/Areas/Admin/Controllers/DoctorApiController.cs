using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Doctor;
using MedicalSystem.Models.DTOs.Password;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Admin.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Authorize(Roles = SD.Role_Admin)]
    [Area("Admin")]
    [Route("api/[area]/[controller]")]
    public class DoctorApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DoctorApiController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllDoctors")]
        public async Task<IActionResult> GetAllDoctors(
        [FromQuery] string searchString = "",
        [FromQuery] string lockoutStatus = "",
        [FromQuery] string currentSort = "",
        [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;

            var (doctors, totalItems) =
                await _unitOfWork.Doctor.GetFilteredDoctorsAsync(
                    searchString, lockoutStatus, null, currentSort, currentPage, pageSize);

            var dtoList = new List<GetAllDoctorsDto>();

            foreach (var d in doctors)
            {
                string displayImageUrl = !string.IsNullOrEmpty(d.ImageUrl)
                    ? d.ImageUrl
                    : d.Gender?.ToLower() == "male"
                        ? "/images/doctors/default-doctor-male.png"
                        : "/images/doctors/default-doctor-female.png";

                var appUserLockoutStatus = await _userManager.IsLockedOutAsync(d.AppUser) ? "Locked" : "Unlocked";

                dtoList.Add(new GetAllDoctorsDto
                {
                    Id = d.Id,
                    DisplayImageUrl = displayImageUrl,
                    FullName = d.FullName,
                    AppUserEmail = d.AppUser.Email,
                    AppUserLockoutStatus = appUserLockoutStatus,
                    CreatedAt = d.CreatedAt
                });
            }

            var response = new
            {
                Doctors = dtoList,
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

        [HttpGet("GetDoctor/{id:int}")]
        public async Task<IActionResult> GetDoctor([FromRoute] int id)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id,
                includeProperties: "AppUser,Department"
            );

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            string displayImageUrl = !string.IsNullOrEmpty(doctor.ImageUrl)
                ? doctor.ImageUrl
                : doctor.Gender?.ToLower() == "male"
                    ? "/images/doctors/default-doctor-male.png"
                    : "/images/doctors/default-doctor-female.png";

            var appUserLockoutStatus = await _userManager.IsLockedOutAsync(doctor.AppUser) ? "Locked" : "Unlocked";

            var dto = new GetDoctorDto
            {
                Id = doctor.Id,
                DisplayImageUrl = displayImageUrl,
                FullName = doctor.FullName,
                AppUserEmail = doctor.AppUser.Email,
                PhoneNumber = doctor.PhoneNumber,
                Address = doctor.Address,
                Gender = doctor.Gender,
                DateOfBirth = doctor.DateOfBirth,
                DepartmentName = doctor.Department?.Name,
                AboutMe = doctor.AboutMe,
                AppUserLockoutStatus = appUserLockoutStatus,
                CreatedAt = doctor.CreatedAt
            };

            return Ok(dto);
        }

        [HttpPost("RegisterDoctor")]
        public async Task<IActionResult> RegisterDoctor([FromForm] RegisterDoctorDto dto)
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

            await _userManager.AddToRoleAsync(appUser, SD.Role_Doctor);

            string? imageUrl = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "doctors");

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

                imageUrl = "/images/doctors/" + fileName + extension;
            }

            var doctor = new MedicalSystem.Models.Models.Doctor
            {
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                DepartmentId = dto.DepartmentId,
                AboutMe = dto.AboutMe,
                ImageUrl = imageUrl,
                AppUserId = appUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Doctor.AddAsync(doctor);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Welcome to the system",
                Message = $"Dear Dr. {doctor.FullName}, your account has been created successfully. " +
                 $"You are now registered under the {doctor.Department?.Name ?? "assigned department"} department.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Doctor registered successfully."
            });
        }

        [HttpPut("EditDoctor/{id:int}")]
        public async Task<IActionResult> EditDoctor([FromRoute] int id, [FromForm] EditDoctorDto dto)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
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
                string uploadPath = Path.Combine(wwwRootPath, "images", "doctors");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(doctor.ImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, doctor.ImageUrl.TrimStart('/'));
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

                doctor.ImageUrl = "/images/doctors/" + fileName + extension;
            }

            doctor.FullName = dto.FullName;
            doctor.PhoneNumber = dto.PhoneNumber;
            doctor.Address = dto.Address;
            doctor.Gender = dto.Gender;
            doctor.DateOfBirth = dto.DateOfBirth;
            doctor.DepartmentId = dto.DepartmentId;
            doctor.AboutMe = dto.AboutMe;

            await _unitOfWork.Doctor.UpdateAsync(doctor);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Profile Updated",
                Message = $"Dear Dr. {doctor.FullName}, your profile has been updated by the admin on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Doctor updated successfully." });
        }

        [HttpDelete("DeleteDoctorImage/{id:int}")]
        public async Task<IActionResult> DeleteDoctorImage([FromRoute] int id)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, doctor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                doctor.ImageUrl = null;
                await _unitOfWork.Doctor.UpdateAsync(doctor);
                await _unitOfWork.SaveAsync();

                return Ok(new { message = "Image deleted successfully." });
            }

            return BadRequest(new { message = "Doctor has no image to delete." });
        }

        [HttpDelete("DeleteDoctor/{id:int}")]
        public async Task<IActionResult> DeleteDoctor([FromRoute] int id)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id,
                includeProperties: "Clinics.Schedules.Appointments,AppUser"
            );

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, doctor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            var notifications = await _unitOfWork.Notification
                .GetAllAsync(n => n.DoctorId == doctor.Id);

            if (notifications.Any())
            {
                _unitOfWork.Notification.RemoveRange(notifications);
            }

            var reviews = await _unitOfWork.DoctorReview
               .GetAllAsync(r => r.DoctorId == doctor.Id);

            if (reviews.Any())
            {
                _unitOfWork.DoctorReview.RemoveRange(reviews);
            }

            foreach (var clinic in doctor.Clinics.ToList())
            {
                foreach (var schedule in clinic.Schedules.ToList())
                {
                    foreach (var appointment in schedule.Appointments.ToList())
                    {
                        var notification = new Notification
                        {
                            Title = "Appointment Cancelled",
                            Message = $"Your appointment with Dr. {doctor.FullName} " +
                                      $"at {clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                                      $"at {appointment.AppointmentTime} has been cancelled because the doctor account was deleted by the admin.",
                            Type = SD.Type_Appointment,
                            Status = SD.Status_Unreaded,
                            PatientId = appointment.PatientId
                        };

                        await _unitOfWork.Notification.AddAsync(notification);
                    }
                }
            }

            _unitOfWork.Doctor.Remove(doctor);
            await _userManager.DeleteAsync(doctor.AppUser);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Doctor account has been deleted successfully." });
        }

        [HttpPost("ToggleDoctorLock/{id:int}")]
        public async Task<IActionResult> ToggleDoctorLock([FromRoute] int id)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id,
                includeProperties: "AppUser"
            );

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            if (await _userManager.IsLockedOutAsync(doctor.AppUser))
            {
                await _userManager.SetLockoutEndDateAsync(doctor.AppUser, null);
                await _userManager.ResetAccessFailedCountAsync(doctor.AppUser);

                await _unitOfWork.SaveAsync();
                var doctorNotification = new Notification
                {
                    Title = "Account Unlocked",
                    Message = $"Dear Dr. {doctor.FullName}, your account has been unlocked by the admin on {DateTime.UtcNow:dddd, dd MMM yyyy}. You can now access the system again.",
                    Type = SD.Type_Account,
                    Status = SD.Status_Unreaded,
                    DoctorId = doctor.Id
                };

                await _unitOfWork.Notification.AddAsync(doctorNotification);
                await _unitOfWork.SaveAsync();
                return Ok(new { message = "Doctor has been unlocked successfully." });
            }
            else
            {
                var lockoutDuration = TimeSpan.FromDays(365 * 10);
                await _userManager.SetLockoutEndDateAsync(doctor.AppUser, DateTimeOffset.UtcNow.Add(lockoutDuration));

                await _unitOfWork.SaveAsync();
                var doctorNotification = new Notification
                {
                    Title = "Account Locked",
                    Message = $"Dear Dr. {doctor.FullName}, your account has been locked by the admin on {DateTime.UtcNow:dddd, dd MMM yyyy}. You will not be able to access the system until it is unlocked.",
                    Type = SD.Type_Account,
                    Status = SD.Status_Unreaded,
                    DoctorId = doctor.Id
                };

                await _unitOfWork.Notification.AddAsync(doctorNotification);
                await _unitOfWork.SaveAsync();
                return Ok(new { message = "Doctor has been locked successfully." });
            }
        }

        [HttpPost("ResetDoctorPassword/{id:int}")]
        public async Task<IActionResult> ResetDoctorPassword([FromRoute] int id, [FromForm] ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest(new { message = "The new password and confirmation password do not match." });
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id, includeProperties: "AppUser");

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(doctor.AppUser);
            var result = await _userManager.ResetPasswordAsync(doctor.AppUser, token, dto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Password reset failed.",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            await _userManager.UpdateSecurityStampAsync(doctor.AppUser);

            var doctorNotification = new Notification
            {
                Title = "Password Reset",
                Message = $"Dear Dr. {doctor.FullName}, your account password has been reset by the admin on {DateTime.UtcNow:dddd, dd MMM yyyy}. Please use your new password to log in.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Password reset successfully.",
                email = doctor.AppUser.Email
            });
        }
    }
}
