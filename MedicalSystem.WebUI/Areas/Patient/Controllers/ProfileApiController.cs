using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Password;
using MedicalSystem.Models.DTOs.Patient;
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
                return NotFound(new { message = "User not found." });

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id,
                includeProperties: "AppUser");

            if (patient == null)
                return NotFound(new { message = "Patient profile not found." });

            // Resolve display image
            string displayImageUrl;
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                displayImageUrl = patient.ImageUrl;
            }
            else
            {
                displayImageUrl = patient.Gender?.ToLower() == "male"
                    ? "/images/patients/default-patient-male.png"
                    : "/images/patients/default-patient-female.png";
            }

            var dto = new GetPatientProfileDto
            {
                DisplayImageUrl = displayImageUrl,
                FullName = patient.FullName,
                AppUserEmail = patient.AppUser.Email,
                PhoneNumber = patient.PhoneNumber,
                Address = patient.Address,
                Gender = patient.Gender,
                DateOfBirth = patient.DateOfBirth,
                CreatedAt = patient.CreatedAt
            };

            return Ok(dto);
        }

        [HttpPut("EditProfile")]
        public async Task<IActionResult> EditProfile([FromForm] EditPatientDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
                return NotFound(new { message = "User not found." });

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id);

            if (patient == null)
                return NotFound(new { message = "Patient profile not found." });

            if (dto.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(dto.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only jpg, jpeg, png images are allowed");

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest("Image size must be less than 2MB");
            }

            patient.FullName = dto.FullName;
            patient.PhoneNumber = dto.PhoneNumber;
            patient.Address = dto.Address;
            patient.Gender = dto.Gender;
            patient.DateOfBirth = dto.DateOfBirth;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "patients");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                if (!string.IsNullOrEmpty(patient.ImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, patient.ImageUrl.TrimStart('/'));
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

                patient.ImageUrl = "/images/patients/" + fileName + extension;
            }

            await _unitOfWork.Patient.UpdateAsync(patient);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Profile Updated",
                Message = $"Your profile information has been updated successfully.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id,
            };

            await _unitOfWork.Notification.AddAsync(notification);
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

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id);

            if (patient == null)
                return NotFound(new { message = "Patient profile not found." });

            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, patient.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);

                patient.ImageUrl = null;
                await _unitOfWork.Patient.UpdateAsync(patient);
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

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id, includeProperties: "AppUser,Appointments.Schedule.Clinic.Doctor");

            if (patient == null)
                return NotFound(new { message = "Patient profile not found." });

            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, patient.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                    System.IO.File.Delete(oldImagePath);
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
                                      $"has deleted their account.",
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

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id,
                includeProperties: "AppUser");

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found." });
            }

            var result = await _userManager.ChangePasswordAsync(
                patient.AppUser, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Password change failed.",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            await _signInManager.RefreshSignInAsync(patient.AppUser);

            var notification = new Notification
            {
                Title = "Password Changed",
                Message = "Your account password has been changed successfully.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Password changed successfully.",
                email = patient.AppUser.Email
            });
        }
    }
}
