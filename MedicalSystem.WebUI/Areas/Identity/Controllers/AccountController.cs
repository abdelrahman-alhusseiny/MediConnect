using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Identity;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Numerics;

namespace MedicalSystem.WebUI.Areas.Identity.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Area("Identity")]
    [Route("Identity/Account")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(
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

        [HttpGet("ChooseRole")]
        public IActionResult ChooseRole()
        {
            var vm = new ChooseRoleVM
            {
                PatientImageUrl = "/images/identity/patient.png",
                DoctorImageUrl = "/images/identity/doctor.png"
            };

            return View("Areas/Identity/Views/Account/ChooseRole.cshtml", vm);
        }

        [HttpGet("RegisterPatient")]
        public IActionResult RegisterPatient()
        {
            var vm = new RegisterPatientVM();
            vm.GenderList = new List<SelectListItem>
        {
            new SelectListItem { Text = "Male", Value = "Male" },
            new SelectListItem { Text = "Female", Value = "Female" },
        };

            return View("Areas/Identity/Views/Account/RegisterPatient.cshtml", vm);
        }

        [HttpPost("RegisterPatient")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPatientConfirmed([FromForm] RegisterPatientVM vm)
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

                return View("Areas/Identity/Views/Account/RegisterPatient.cshtml", vm);
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

                return View("Areas/Identity/Views/Account/RegisterPatient.cshtml", vm);
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

            TempData["success"] = "Patient account registered successfully.";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        [HttpGet("RegisterDoctor")]
        public async Task<IActionResult> RegisterDoctor()
        {
            var vm = new RegisterDoctorVM();
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Text = "Male", Value = "Male" },
                new SelectListItem { Text = "Female", Value = "Female" },
            };
            vm.DepartmentList = (await _unitOfWork.Department.GetAllAsync()).Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                });

            return View("Areas/Identity/Views/Account/RegisterDoctor.cshtml", vm);
        }

        [HttpPost("RegisterDoctor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterDoctorConfirmed([FromForm] RegisterDoctorVM vm)
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
                    c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    });

                return View("Areas/Identity/Views/Account/RegisterDoctor.cshtml", vm);
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
                    c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    });

                return View("Areas/Identity/Views/Account/RegisterDoctor.cshtml", vm);
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
                Title = "Welcome to the System",
                Message = $"Dear Dr. {vm.Doctor.FullName}, your account has been created successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}. You can now log in and start using the platform.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = vm.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Doctor account registered successfully.";
            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        [HttpGet("Login")]
        public IActionResult Login(string? returnUrl = null)
        {
            var vm = new LoginVM { ReturnUrl = returnUrl };
            return View("Areas/Identity/Views/Account/Login.cshtml", vm);
        }

        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginConfirmed([FromForm] LoginVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View("Areas/Identity/Views/Account/Login.cshtml", vm);
            }

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt");
                return View("Areas/Identity/Views/Account/Login.cshtml", vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                vm.Password,
                vm.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                TempData["success"] = "You have logged in successfully.";
                return await RedirectToLocal(vm.ReturnUrl, user);
            }
            else
            {
                if (result.IsLockedOut)
                {
                    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                    ModelState.AddModelError(string.Empty,
                        $"Your account is locked until {lockoutEnd?.LocalDateTime}");

                    return View("Areas/Identity/Views/Account/Login.cshtml", vm);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt");
                return View("Areas/Identity/Views/Account/Login.cshtml", vm);
            }
        }

        private async Task<IActionResult> RedirectToLocal(
            string? returnUrl, ApplicationUser user)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(SD.Role_Admin))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (roles.Contains(SD.Role_Patient))
                return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
            if (roles.Contains(SD.Role_Doctor))
                return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
