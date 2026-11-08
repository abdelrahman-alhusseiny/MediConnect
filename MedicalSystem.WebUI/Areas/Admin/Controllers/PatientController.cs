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
    [Route("Admin/Patient")]
    public class PatientController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PatientController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
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

            var patientRows = new List<PatientRowVM>();
            foreach (var p in patients)
            {
                string displayImageUrl;
                if (!string.IsNullOrEmpty(p.ImageUrl))
                {
                    displayImageUrl = p.ImageUrl;
                }
                else
                {
                    displayImageUrl = p.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
                }
                var appUserLockoutStatus = await _userManager.IsLockedOutAsync(p.AppUser) ? "Locked" : "Unlocked";
                patientRows.Add(new PatientRowVM
                {
                    Patient = p,
                    DisplayImageUrl = displayImageUrl,
                    AppUserEmail = p.AppUser.Email,
                    AppUserLockoutStatus = appUserLockoutStatus
                });
            }

            var vm = new PatientListVM
            {
                Patients = patientRows,
                SourceView = "GetAllPatients",

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

            return View("Areas/Admin/Views/Patient/GetAllPatients.cshtml", vm);
        }

        [HttpGet("GetPatient/{id:int}")]
        public async Task<IActionResult> GetPatient(
            [FromRoute] int id, [FromQuery] PatientVM vm)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            vm.Patient = patient;
            vm.SourceView = "GetPatient";
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                vm.DisplayImageUrl = patient.ImageUrl;
            }
            else
            {
                vm.DisplayImageUrl = patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }
            vm.AppUserEmail = patient.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(patient.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Patient/GetPatient.cshtml", vm);
        }

        [HttpGet("RegisterPatient")]
        public IActionResult RegisterPatient([FromQuery] RegisterPatientVM vm)
        {
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" },
            };

            return View("Areas/Admin/Views/Patient/RegisterPatient.cshtml", vm);
        }

        [HttpPost("RegisterPatient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPatientConfirmed([FromForm] RegisterPatientVM vm)
        {
            if (vm.Patient.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Patient.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Patient.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Patient.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Patient.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" },
                };

                return View("Areas/Admin/Views/Patient/RegisterPatient.cshtml", vm);
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

                return View("Areas/Admin/Views/Patient/RegisterPatient.cshtml", vm);
            }

            await _userManager.AddToRoleAsync(appUser, SD.Role_Patient);

            if (vm.Patient.ImageFile != null && vm.Patient.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "patients");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
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

            vm.Patient.AppUserId = appUser.Id;

            await _unitOfWork.Patient.AddAsync(vm.Patient);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Account Created",
                Message = $"Welcome {vm.Patient.FullName}, your patient account has been created successfully.",
                Type = SD.Type_Account, 
                Status = SD.Status_Unreaded,
                PatientId = vm.Patient.Id
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Patient registered successfully.";
            return RedirectToAction("GetAllPatients", "Patient", new { area = "Admin" });
        }

        [HttpGet("EditPatient/{id:int}")]
        public async Task<IActionResult> EditPatient(
            [FromRoute] int id, [FromQuery] PatientVM vm)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            vm.Patient = patient;
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" }
            };
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                vm.DisplayImageUrl = patient.ImageUrl;
            }
            else
            {
                vm.DisplayImageUrl = patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }
            vm.HasCustomImage = !string.IsNullOrEmpty(vm.Patient.ImageUrl);

            return View("Areas/Admin/Views/Patient/EditPatient.cshtml", vm);
        }

        [HttpPost("EditPatient/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatientConfirmed([FromForm] PatientVM vm)
        {
            if (vm.Patient.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Patient.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Patient.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Patient.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Patient.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Text = "Male", Value = "Male" },
                    new SelectListItem { Text = "Female", Value = "Female" }
                };

                return View("Areas/Admin/Views/Patient/EditPatient.cshtml", vm);
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
                    string oldImagePath = Path.Combine(wwwRootPath, vm.Patient.ImageUrl.TrimStart('/'));
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
                Title = "Account Updated",
                Message = $"Patient account for {vm.Patient.FullName} has been updated successfully.",
                Type = SD.Type_Account, 
                Status = SD.Status_Unreaded,
                PatientId = vm.Patient.Id,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Patient updated successfully.";
            return RedirectToAction("GetPatient", "Patient", new
            {
                area = "Admin",
                id = vm.Patient.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeletePatientImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatientImage([FromForm] PatientVM vm)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.Id == vm.Patient.Id);
            if (patient == null)
            {
                return NotFound();
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
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditPatient", "Patient", new
            {
                area = "Admin",
                id = vm.Patient.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeletePatient/{id:int}")]
        public async Task<IActionResult> DeletePatient([FromRoute] int id, [FromQuery] PatientVM vm)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            vm.Patient = patient;
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                vm.DisplayImageUrl = patient.ImageUrl;
            }
            else
            {
                vm.DisplayImageUrl = patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }
            vm.AppUserEmail = patient.AppUser.Email;
            vm.AppUserLockoutStatus = await _userManager.IsLockedOutAsync(patient.AppUser) ? "Locked" : "Unlocked";

            return View("Areas/Admin/Views/Patient/DeletePatient.cshtml", vm);
        }

        [HttpPost("DeletePatient/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatientConfirmed([FromForm] PatientVM vm)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == vm.Patient.Id, includeProperties: "AppUser,Appointments.Schedule.Clinic.Doctor");
            if (patient == null)
            {
                return NotFound();
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

            TempData["success"] = "Admin account has been deleted successfully.";
            return RedirectToAction("GetAllPatients", "Patient", new
            {
                area = "Admin",
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("TogglePatientLock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePatientLock([FromForm] PatientVM vm, string SourceView)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
               p => p.Id == vm.Patient.Id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            Notification notification;

            if (await _userManager.IsLockedOutAsync(patient.AppUser))
            {
                await _userManager.SetLockoutEndDateAsync(patient.AppUser, null);
                await _userManager.ResetAccessFailedCountAsync(patient.AppUser);
                TempData["success"] = $"Patient has been unlocked successfully.";

                notification = new Notification
                {
                    Title = "Account Unlocked",
                    Message = $"Your patient account has been unlocked. You can now log in again.",
                    Type = SD.Type_Account,
                    Status = SD.Status_Unreaded,
                    PatientId = patient.Id,
                };
            }
            else
            {
                var lockoutDuration = TimeSpan.FromDays(365 * 10);
                await _userManager.SetLockoutEndDateAsync(patient.AppUser, DateTimeOffset.UtcNow.Add(lockoutDuration));
                TempData["success"] = $"Patient has been locked successfully.";

                notification = new Notification
                {
                    Title = "Account Locked",
                    Message = $"Your patient account has been locked by the administrator. Please contact support for assistance.",
                    Type = SD.Type_Account,
                    Status = SD.Status_Unreaded,
                    PatientId = patient.Id,
                };
            }

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            return RedirectToAction(vm.SourceView, "Patient", new
            {
                area = "Admin",
                id = vm.Patient.Id,
                searchString = vm.SearchString,
                lockoutStatus = vm.LockoutStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("ResetPatientPassword/{id:int}")]
        public async Task<IActionResult> ResetPatientPassword([FromRoute] int id, [FromQuery] ResetPasswordVM vm)
        {
            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            vm.Id = patient.Id;
            vm.AppUserEmail = patient.AppUser.Email;
            return View("Areas/Admin/Views/Patient/ResetPatientPassword.cshtml", vm);
        }

        [HttpPost("ResetPatientPassword/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPatientPasswordConfirmed([FromForm] ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Admin/Views/Patient/ResetPatientPassword.cshtml", vm);
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(
                p => p.Id == vm.Id, includeProperties: "AppUser");
            if (patient == null)
            {
                return NotFound();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(patient.AppUser);
            var result = await _userManager.ResetPasswordAsync(patient.AppUser, token, vm.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View("Areas/Admin/Views/Patient/ResetPatientPassword.cshtml", vm);
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

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Password changed successfully.";
            return RedirectToAction(vm.SourceView, "Patient", new
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
