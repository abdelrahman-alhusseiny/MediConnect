using ECommerce.Models.ViewModels;
using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Numerics;

namespace MedicalSystem.WebUI.Areas.Doctor.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Doctor)]
    [Area("Doctor")]
    [Route("Doctor/Profile")]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(
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
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id, includeProperties: "AppUser,Department");
            if (doctor == null)
            {
                return NotFound();
            }

            string displayImageUrl;
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                displayImageUrl = doctor.ImageUrl;
            }
            else
            {
                displayImageUrl = doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }

            var vm = new DoctorVM
            {
                Doctor = doctor,
                DisplayImageUrl = displayImageUrl,
                AppUserEmail = doctor.AppUser.Email,
                DepartmentName = doctor.Department.Name,
            };

            return View("Areas/Doctor/Views/Profile/GetProfile.cshtml", vm);
        }

        [HttpGet("EditProfile")]
        public async Task<IActionResult> EditProfile()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound();
            }

            string displayImageUrl;
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                displayImageUrl = doctor.ImageUrl;
            }
            else
            {
                displayImageUrl = doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }

            var vm = new DoctorVM
            {
                Doctor = doctor,
                GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                },
                DepartmentList = (await _unitOfWork.Department.GetAllAsync()).Select(
                    d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToList(),
                DisplayImageUrl = displayImageUrl,
                HasCustomImage = !string.IsNullOrEmpty(doctor.ImageUrl)
            };

            return View("Areas/Doctor/Views/Profile/EditProfile.cshtml", vm);
        }

        [HttpPost("EditProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfileConfirmed([FromForm] DoctorVM vm)
        {
            if (vm.Doctor.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension =
                    Path.GetExtension(vm.Doctor.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError(
                        "Doctor.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Doctor.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError(
                        "Doctor.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                };
                vm.DepartmentList = (await _unitOfWork.Department.GetAllAsync()).Select(
                    d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    });
               
                return View("Areas/Doctor/Views/Profile/EditProfile.cshtml", vm);
            }

            if (vm.Doctor.ImageFile != null && vm.Doctor.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "doctors");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(vm.Doctor.ImageUrl))
                {
                    string oldImagePath = Path.Combine(
                        wwwRootPath, vm.Doctor.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Doctor.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Doctor.ImageFile.CopyToAsync(fileStream);
                }

                vm.Doctor.ImageUrl = "/images/doctors/" + fileName + extension;
            }

            await _unitOfWork.Doctor.UpdateAsync(vm.Doctor);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Profile Updated",
                Message = $"Dear Dr. {vm.Doctor.FullName}, you updated your profile successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_Account, 
                Status = SD.Status_Unreaded,
                DoctorId = vm.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Profile updated successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Doctor" });
        }

        [HttpPost("DeleteProfileImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, doctor.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                doctor.ImageUrl = null;
                await _unitOfWork.Doctor.UpdateAsync(doctor);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditProfile", "Profile", new { area = "Doctor" });
        }

        [HttpGet("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id, includeProperties: "AppUser,Department");
            if (doctor == null)
            {
                return NotFound();
            }

            string displayImageUrl;
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                displayImageUrl = doctor.ImageUrl;
            }
            else
            {
                displayImageUrl = doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }

            var vm = new DoctorVM
            {
                Doctor = doctor,
                DisplayImageUrl = displayImageUrl,
                AppUserEmail = doctor.AppUser.Email,
                DepartmentName = doctor.Department.Name,
            };

            return View("Areas/Doctor/Views/Profile/DeleteAccount.cshtml", vm);
        }

        [HttpPost("DeleteAccount")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id, includeProperties: "Clinics.Schedules.Appointments");
            if (doctor == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, doctor.ImageUrl.TrimStart('/'));

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

            TempData["success"] = "Your account has been deleted successfully.";
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpGet("ChangeAccountPassword")]
        public async Task<IActionResult> ChangeAccountPassword()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (doctor == null)
            {
                return NotFound();
            }

            var vm = new ChangePasswordVM
            {
                AppUserEmail = doctor.AppUser.Email
            };

            return View("Areas/Doctor/Views/Profile/ChangeAccountPassword.cshtml", vm);
        }

        [HttpPost("ChangeAccountPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAccountPasswordConfirmed([FromForm] ChangePasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Doctor/Views/Profile/ChangeAccountPassword.cshtml", vm);
            }

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (doctor == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(doctor.AppUser, vm.CurrentPassword, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View("Areas/Doctor/Views/Profile/ChangeAccountPassword.cshtml", vm);
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

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Doctor" });
        }
    }
}
