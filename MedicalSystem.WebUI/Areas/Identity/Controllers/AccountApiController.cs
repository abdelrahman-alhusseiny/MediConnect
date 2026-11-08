using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Doctor;
using MedicalSystem.Models.DTOs.Identity;
using MedicalSystem.Models.DTOs.Patient;
using MedicalSystem.Models.Models;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedicalSystem.WebUI.Areas.Identity.Controllers
{
    [ApiExplorerSettings(GroupName = "v1")]
    [ApiController]
    [Route("api/[area]/[controller]")]
    [Area("Identity")]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountApiController(
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
        public ActionResult<ChooseRoleDto> ChooseRole()
        {
            var dto = new ChooseRoleDto
            {
                PatientImageUrl = "/images/identity/patient.png",
                DoctorImageUrl = "/images/identity/doctor.png"
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
                    return BadRequest("Only jpg, jpeg, png images are allowed");

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest("Image size must be less than 2MB");
            }

            var appUser = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(appUser, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(appUser, SD.Role_Patient);

            string? imageUrl = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "patients");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

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
                message = "Patient account registered successfully.",
            });
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
                    return BadRequest("Only jpg, jpeg, png images are allowed");

                if (dto.ImageFile.Length > 2 * 1024 * 1024)
                    return BadRequest("Image size must be less than 2MB");
            }

            var appUser = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(appUser, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(appUser, SD.Role_Doctor);

            string? imageUrl = null;
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "doctors");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

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
                Title = "Welcome to the System",
                Message = $"Dear Dr. {doctor.FullName}, your account has been created successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}. You can now log in and start using the platform.",
                Type = SD.Type_Account,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Doctor account registered successfully.",
            });
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
                return BadRequest("Email and password are required.");

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid login attempt.");

            var result = await _signInManager.PasswordSignInAsync(
                user,
                dto.Password,
                dto.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                    message = "You have logged in successfully.",
                    userId = user.Id,
                    email = user.Email,
                    roles
                });
            }

            if (result.IsLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                return Unauthorized(new
                {
                    message = $"Your account is locked until {lockoutEnd?.LocalDateTime}"
                });
            }

            return Unauthorized("Invalid login attempt.");
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return Ok(new
            {
                message = "You have been logged out successfully."
            });
        }
    }
}
