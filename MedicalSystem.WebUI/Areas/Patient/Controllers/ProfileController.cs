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

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("Patient/Profile")]
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

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            string displayImageUrl;
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                displayImageUrl = patient.ImageUrl;
            }
            else
            {
                displayImageUrl = patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }

            var patientVM = new PatientVM
            {
                Patient = patient,
                DisplayImageUrl = displayImageUrl,
                AppUserEmail = patient.AppUser.Email
            };

            return View("Areas/Patient/Views/Profile/GetProfile.cshtml", patientVM);
        }

        [HttpGet("EditProfile")]
        public async Task<IActionResult> EditProfile()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound();
            }

            string displayImageUrl;
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                displayImageUrl = patient.ImageUrl;
            }
            else
            {
                displayImageUrl = patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }

            var patientVM = new PatientVM
            {
                Patient = patient,
                GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                },
                DisplayImageUrl = displayImageUrl,
                HasCustomImage = !string.IsNullOrEmpty(patient.ImageUrl)
            };

            return View("Areas/Patient/Views/Profile/EditProfile.cshtml", patientVM);
        }

        [HttpPost("EditProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfileConfirmed([FromForm] PatientVM vm)
        {
            if (vm.Patient.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension =
                    Path.GetExtension(vm.Patient.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError(
                        "Patient.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Patient.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError(
                        "Patient.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                };

                return View("Areas/Patient/Views/Profile/EditProfile.cshtml", vm);
            }

            if (vm.Patient.ImageFile != null && vm.Patient.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "patients");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(vm.Patient.ImageUrl))
                {
                    string oldImagePath = Path.Combine(
                        wwwRootPath, vm.Patient.ImageUrl.TrimStart('/'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Patient.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Patient.ImageFile.CopyToAsync(fileStream);
                }

                vm.Patient.ImageUrl = "/images/patients/" + fileName + extension;
            }

            await _unitOfWork.Patient.UpdateAsync(vm.Patient);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Profile Updated",
                Message = $"Your profile information has been updated successfully.",
                Type = SD.Type_Account, 
                Status = SD.Status_Unreaded,
                PatientId = vm.Patient.Id,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Profile updated successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Patient" });
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

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, patient.ImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                patient.ImageUrl = null;
                await _unitOfWork.Patient.UpdateAsync(patient);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditProfile", "Profile", new { area = "Patient" });
        }

        [HttpGet("DeleteAccount")]
        public async Task<IActionResult> DeleteAccount()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            string displayImageUrl;
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                displayImageUrl = patient.ImageUrl;
            }
            else
            {
                displayImageUrl = patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }

            var patientVM = new PatientVM
            {
                Patient = patient,
                DisplayImageUrl = displayImageUrl,
                AppUserEmail = patient.AppUser.Email
            };

            return View("Areas/Patient/Views/Profile/DeleteAccount.cshtml", patientVM);
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

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id, includeProperties: "Appointments.Schedule.Clinic.Doctor");
            if (patient == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(
                    wwwRootPath, patient.ImageUrl.TrimStart('/'));

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

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            var vm = new ChangePasswordVM
            {
                AppUserEmail = patient.AppUser.Email
            };

            return View("Areas/Patient/Views/Profile/ChangeAccountPassword.cshtml", vm);
        }

        [HttpPost("ChangeAccountPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAccountPasswordConfirmed([FromForm] ChangePasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Patient/Views/Profile/ChangeAccountPassword.cshtml", vm);
            }

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.AppUserId == appUser.Id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(patient.AppUser, vm.CurrentPassword, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View("Areas/Patient/Views/Profile/ChangeAccountPassword.cshtml", vm);
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

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction("GetProfile", "Profile", new { area = "Patient" });
        }
    }
}
