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

namespace MedicalSystem.WebUI.Areas.Admin.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Admin)]
    [Area("Admin")]
    [Route("Admin/Doctor")]
    public class DoctorController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DoctorController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
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

            var doctorRows = new List<DoctorRowVM>();
            foreach (var d in doctors)
            {
                string displayImageUrl;
                if (!string.IsNullOrEmpty(d.ImageUrl))
                {
                    displayImageUrl = d.ImageUrl;
                }
                else
                {
                    displayImageUrl = d.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
                }
                var appUserLockoutStatus = await _userManager.IsLockedOutAsync(d.AppUser) ? "Locked" : "Unlocked";
                doctorRows.Add(new DoctorRowVM
                {
                    Doctor = d,
                    DisplayImageUrl = displayImageUrl,
                    AppUserEmail = d.AppUser.Email,
                    AppUserLockoutStatus = appUserLockoutStatus
                });
            }

            var vm = new DoctorListVM
            {
                Doctors = doctorRows,
                SourceView = "GetAllDoctors",

                SearchString = searchString,

                LockoutStatus = lockoutStatus,
                LockoutStatusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Locked", Text = "Locked Accounts", Selected = lockoutStatus == "Locked" },
                    new SelectListItem { Value = "Unlocked", Text = "Unlocked Accounts", Selected = lockoutStatus == "Unlocked" }
                },

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Admin/Views/Doctor/GetAllDoctors.cshtml", vm);
        }

        [HttpGet("GetDoctor/{id:int}")]
        public async Task<IActionResult> GetDoctor(
            [FromRoute] int id, [FromQuery] DoctorVM vm)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id, includeProperties: "AppUser,Department");
            if (doctor == null)
            {
                return NotFound();
            }

            vm.Doctor = doctor;
            vm.SourceView = "GetDoctor";
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                vm.DisplayImageUrl = doctor.ImageUrl;
            }
            else
            {
                vm.DisplayImageUrl = doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }
            vm.AppUserEmail = doctor.AppUser.Email;
            vm.DepartmentName = doctor.Department.Name;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(doctor.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Doctor/GetDoctor.cshtml", vm);
        }

        [HttpGet("RegisterDoctor")]
        public async Task<IActionResult> RegisterDoctor([FromQuery] RegisterDoctorVM vm)
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

            return View("Areas/Admin/Views/Doctor/RegisterDoctor.cshtml", vm);
        }

        [HttpPost("RegisterDoctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctorConfirmed([FromForm] RegisterDoctorVM vm)
        {
            if (vm.Doctor.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Doctor.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Doctor.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Doctor.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Doctor.ImageFile", "Image size must be less than 2MB");
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

                return View("Areas/Admin/Views/Doctor/RegisterDoctor.cshtml", vm);
            }

            var appUser = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email
            };

            var result = await _userManager.CreateAsync(appUser, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

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

                return View("Areas/Admin/Views/Doctor/RegisterDoctor.cshtml", vm);
            }

            await _userManager.AddToRoleAsync(appUser, SD.Role_Doctor);

            if (vm.Doctor.ImageFile != null && vm.Doctor.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "doctors");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
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

            vm.Doctor.AppUserId = appUser.Id;

            await _unitOfWork.Doctor.AddAsync(vm.Doctor);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Welcome to the system",
                Message = $"Dear Dr. {vm.Doctor.FullName}, your account has been created successfully. " +
                  $"You are now registered under the {vm.Doctor.Department?.Name ?? "assigned department"} department.",
                Type = SD.Type_Account, 
                Status = SD.Status_Unreaded,
                DoctorId = vm.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Doctor registered successfully.";
            return RedirectToAction("GetAllDoctors", "Doctor", new { area = "Admin" });
        }

        [HttpGet("EditDoctor/{id:int}")]
        public async Task<IActionResult> EditDoctor(
            [FromRoute] int id, [FromQuery] DoctorVM vm)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id, includeProperties: "AppUser");
            if (doctor == null)
            {
                return NotFound();
            }

            vm.Doctor = doctor;
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" }
            };
            vm.DepartmentList = (await _unitOfWork.Department.GetAllAsync()).Select(
            d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            });
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                vm.DisplayImageUrl = doctor.ImageUrl;
            }
            else
            {
                vm.DisplayImageUrl = doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }
            vm.HasCustomImage = !string.IsNullOrEmpty(doctor.ImageUrl);

            return View("Areas/Admin/Views/Doctor/EditDoctor.cshtml", vm);
        }

        [HttpPost("EditDoctor/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctorConfirmed([FromForm] DoctorVM vm)
        {
            if (vm.Doctor.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Doctor.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Doctor.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Doctor.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Doctor.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" }
                };
                vm.DepartmentList = (await _unitOfWork.Department.GetAllAsync()).Select(
                d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                });

                return View("Areas/Admin/Views/Doctor/EditDoctor.cshtml", vm);
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
                    string oldImagePath = Path.Combine(wwwRootPath, vm.Doctor.ImageUrl.TrimStart('/'));
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
                Message = $"Dear Dr. {vm.Doctor.FullName}, your profile has been updated by the admin on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_Account, 
                Status = SD.Status_Unreaded,
                DoctorId = vm.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Doctor updated successfully.";
            return RedirectToAction("GetDoctor", "Doctor", new
            {
                area = "Admin",
                id = vm.Doctor.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeleteDoctorImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorImage([FromForm] DoctorVM vm)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.Id == vm.Doctor.Id);
            if (doctor == null)
            {
                return NotFound();
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
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditDoctor", "Doctor", new
            {
                area = "Admin",
                id = vm.Doctor.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteDoctor/{id:int}")]
        public async Task<IActionResult> DeleteDoctor([FromRoute] int id, [FromQuery] DoctorVM vm)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id, includeProperties: "AppUser,Department");
            if (doctor == null)
            {
                return NotFound();
            }

            vm.Doctor = doctor;
            if (!string.IsNullOrEmpty(doctor.ImageUrl))
            {
                vm.DisplayImageUrl = doctor.ImageUrl;
            }
            else
            {
                vm.DisplayImageUrl = doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }
            vm.AppUserEmail = doctor.AppUser.Email;
            vm.DepartmentName = doctor.Department.Name;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(doctor.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Doctor/DeleteDoctor.cshtml", vm);
        }

        [HttpPost("DeleteDoctor/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorConfirmed([FromForm] DoctorVM vm)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == vm.Doctor.Id, includeProperties: "Clinics.Schedules.Appointments,AppUser");
            if (doctor == null)
            {
                return NotFound();
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

            TempData["success"] = "Doctor account has been deleted successfully.";
            return RedirectToAction("GetAllDoctors", "Doctor", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("ToggleDoctorLock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDoctorLock([FromForm] DoctorVM vm, string SourceView)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
               d => d.Id == vm.Doctor.Id, includeProperties: "AppUser");
            if (doctor == null)
            {
                return NotFound();
            }

            bool isLocked;
            if (await _userManager.IsLockedOutAsync(doctor.AppUser))
            {
                await _userManager.SetLockoutEndDateAsync(doctor.AppUser, null);
                await _userManager.ResetAccessFailedCountAsync(doctor.AppUser);
                TempData["success"] = $"Doctor has been unlocked successfully.";
                isLocked = false;
            }
            else
            {
                var lockoutDuration = TimeSpan.FromDays(365 * 10);
                await _userManager.SetLockoutEndDateAsync(doctor.AppUser, DateTimeOffset.UtcNow.Add(lockoutDuration));
                TempData["success"] = $"Doctor has been locked successfully.";
                isLocked = true;
            }

            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = isLocked ? "Account Locked" : "Account Unlocked",
                Message = isLocked
                    ? $"Dear Dr. {doctor.FullName}, your account has been locked by the admin on {DateTime.UtcNow:dddd, dd MMM yyyy}. You will not be able to access the system until it is unlocked."
                    : $"Dear Dr. {doctor.FullName}, your account has been unlocked by the admin on {DateTime.UtcNow:dddd, dd MMM yyyy}. You can now access the system again.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return RedirectToAction(vm.SourceView, "Doctor", new
            {
                area = "Admin",
                id = vm.Doctor.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("ResetDoctorPassword/{id:int}")]
        public async Task<IActionResult> ResetDoctorPassword([FromRoute] int id, [FromQuery] ResetPasswordVM vm)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == id, includeProperties: "AppUser");
            if (doctor == null)
            {
                return NotFound();
            }

            vm.Id = doctor.Id;
            vm.AppUserEmail = doctor.AppUser.Email;
            return View("Areas/Admin/Views/Doctor/ResetDoctorPassword.cshtml", vm);
        }

        [HttpPost("ResetDoctorPassword/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDoctorPasswordConfirmed([FromForm] ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Doctor/ResetDoctorPassword.cshtml", vm);
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == vm.Id, includeProperties: "AppUser");
            if (doctor == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(doctor.AppUser);
            var result = await _userManager.ResetPasswordAsync(doctor.AppUser, token, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View("Areas/Admin/Views/Doctor/ResetDoctorPassword.cshtml", vm);
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

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction(vm.SourceView, "Doctor", new
            {
                area = "Admin",
                id = vm.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
