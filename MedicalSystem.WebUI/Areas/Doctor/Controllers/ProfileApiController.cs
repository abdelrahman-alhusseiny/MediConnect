using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Doctor;
using MedicalSystem.Models.DTOs.Password;
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
    public class ProfileApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id,
                includeProperties: "AppUser,Department");

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor profile not found." });
            }

            string displayImageUrl;
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                displayImageUrl = doctor.ImageUrl;
            }
            else
            {
                displayImageUrl = doctor.Gender?.ToLower() == "male"
                    ? "/images/doctors/default-doctor-male.png"
                    : "/images/doctors/default-doctor-female.png";
            }

            var dto = new GetDoctorProfileDto
            {
                DisplayImageUrl = displayImageUrl,
                FullName = doctor.FullName,
                AppUserEmail = doctor.AppUser.Email,
                PhoneNumber = doctor.PhoneNumber,
                Address = doctor.Address,
                Gender = doctor.Gender,
                DateOfBirth = doctor.DateOfBirth,
                DepartmentName = doctor.Department.Name,
                AboutMe = doctor.AboutMe,
                CreatedAt = doctor.CreatedAt
            };

            return Ok(dto);
        }

        [HttpPut("EditProfile")]
        public async Task<IActionResult> EditProfile([FromForm] EditDoctorDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return NotFound(new { message = "User not found." });

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);

            if (doctor == null)
                return NotFound(new { message = "Doctor profile not found." });

            if (dto.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only jpg, jpeg, png images are allowed");

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest("Image size must be less than 2MB");
            }

            doctor.FullName = dto.FullName;
            doctor.PhoneNumber = dto.PhoneNumber;
            doctor.Address = dto.Address;
            doctor.Gender = dto.Gender;
            doctor.DateOfBirth = dto.DateOfBirth;
            doctor.DepartmentId = dto.DepartmentId;
            doctor.AboutMe = dto.AboutMe;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "doctors");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                if (!string.IsNullOrEmpty(doctor.ImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, doctor.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
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

            await _unitOfWork.Doctor.UpdateAsync(doctor);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Profile Updated",
                Message = $"Dear Dr. {doctor.FullName}, you updated your profile successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
            });
        }

        [HttpDelete("DeleteProfileImage")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return NotFound(new { message = "User not found." });

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);

            if (doctor == null)
                return NotFound(new { message = "Doctor profile not found." });

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

            return BadRequest(new { message = "No image found to delete." });
        }

        [HttpDelete("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return NotFound(new { message = "User not found." });

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id, includeProperties: "Clinics.Schedules.Appointments,AppUser,Department");

            if (doctor == null)
                return NotFound(new { message = "Doctor profile not found." });

            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, doctor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
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
                                      $"at {appointment.AppointmentTime} has been cancelled because the doctor deleted their account.",
                            Type = SD.Type_Appointment,
                            Status = SD.Status_Unreaded,
                            PatientId = appointment.PatientId
                        };

                        await _unitOfWork.Notification.AddAsync(notification);
                    }
                }
            }

            _unitOfWork.Doctor.Remove(doctor);
            await _userManager.DeleteAsync(appUser);
            await _unitOfWork.SaveAsync();
            await _signInManager.SignOutAsync();

            return Ok(new { message = "Your account has been deleted successfully." });
        }

        [HttpPost("ChangeAccountPassword")]
        public async Task<IActionResult> ChangeAccountPassword([FromForm] ChangePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest(new { message = "The new password and confirmation password do not match." });
            }

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return NotFound(new { message = "User not found." });

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id,
                includeProperties: "AppUser");

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            var result = await _userManager.ChangePasswordAsync(
                doctor.AppUser, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Password change failed.",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            await _signInManager.RefreshSignInAsync(doctor.AppUser);

            var doctorNotification = new Notification
            {
                Title = "Password Changed",
                Message = $"Dear Dr. {doctor.FullName}, you successfully changed your account password on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Password changed successfully.",
                email = doctor.AppUser.Email
            });
        }
    }
}
