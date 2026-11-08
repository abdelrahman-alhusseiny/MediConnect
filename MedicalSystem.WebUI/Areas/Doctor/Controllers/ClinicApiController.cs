using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Clinic;
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
    public class ClinicApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public ClinicApiController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetAllClinics")]
        public async Task<IActionResult> GetAllClinics(
        [FromQuery] string searchString = "",
        [FromQuery] string currentSort = "",
        [FromQuery] int currentPage = 1)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            int pageSize = 6;
            var (clinics, totalItems) =
                await _unitOfWork.Clinic.GetFilteredClinicsAsync(
                    doctor.Id, searchString, currentSort, currentPage, pageSize);

            var dtoList = clinics.Select(c => new GetClinicDto
            {
                Id = c.Id,
                Name = c.Name,
                Address = c.Address,
                PhoneNumber = c.PhoneNumber,
                ConsultationPrice = c.ConsultationPrice,
                CreatedAt = c.CreatedAt
            }).ToList();

            var response = new
            {
                Clinics = dtoList,
                SearchString = searchString,
                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(response);
        }

        [HttpGet("GetClinic/{id:int}")]
        public async Task<IActionResult> GetClinic([FromRoute] int id)
        {
            var clinic = await _unitOfWork.Clinic.GetFirstOrDefaultAsync(c => c.Id == id);
            if (clinic == null)
            {
                return NotFound(new { message = "Clinic not found." });
            }

            var dto = new GetClinicDto
            {
                Id = clinic.Id,
                Name = clinic.Name,
                Address = clinic.Address,
                PhoneNumber = clinic.PhoneNumber,
                ConsultationPrice = clinic.ConsultationPrice,
                CreatedAt = clinic.CreatedAt
            };

            return Ok(dto);
        }

        [HttpPost("CreateClinic")]
        public async Task<IActionResult> CreateClinic([FromForm] ClinicDto dto)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found." });
            }

            if (await _unitOfWork.Clinic.IsNameExistsAsync(dto.Name))
            {
                return BadRequest(new { message = "This clinic name already exists." });
            }

            var clinic = new Clinic
            {
                Name = dto.Name,
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
                ConsultationPrice = dto.ConsultationPrice,
                DoctorId = doctor.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Clinic.AddAsync(clinic);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Clinic Created",
                Message = $"Dear Dr. {doctor.FullName}, your new clinic \"{clinic.Name}\" was created successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Clinic created successfully.",
            });
        }

        [HttpPut("EditClinic/{id:int}")]
        public async Task<IActionResult> EditClinic([FromRoute] int id, [FromForm] ClinicDto dto)
        {
            var clinic = await _unitOfWork.Clinic.GetFirstOrDefaultAsync(c => c.Id == id);
            if (clinic == null)
            {
                return NotFound(new { message = "Clinic not found." });
            }

            if (await _unitOfWork.Clinic.IsNameExistsAsync(dto.Name, id))
            {
                return BadRequest(new { message = "This clinic name already exists." });
            }

            clinic.Name = dto.Name;
            clinic.Address = dto.Address;
            clinic.PhoneNumber = dto.PhoneNumber;
            clinic.ConsultationPrice = dto.ConsultationPrice;

            await _unitOfWork.Clinic.UpdateAsync(clinic);
            await _unitOfWork.SaveAsync();

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == clinic.DoctorId);

            if (doctor != null)
            {
                var doctorNotification = new Notification
                {
                    Title = "Clinic Updated",
                    Message = $"Dear Dr. {doctor.FullName}, your clinic \"{clinic.Name}\" was updated successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                    Type = SD.Type_System,
                    Status = SD.Status_Unreaded,
                    DoctorId = doctor.Id,
                };

                await _unitOfWork.Notification.AddAsync(doctorNotification);
                await _unitOfWork.SaveAsync();
            }

            return Ok(new { message = "Clinic updated successfully." });
        }

        [HttpDelete("DeleteClinic/{id:int}")]
        public async Task<IActionResult> DeleteClinic([FromRoute] int id)
        {
            var clinic = await _unitOfWork.Clinic.GetFirstOrDefaultAsync(c => c.Id == id, includeProperties: "Doctor,Schedules.Appointments");
            if (clinic == null)
            {
                return NotFound(new { message = "Clinic not found." });
            }

            foreach (var schedule in clinic.Schedules.ToList())
            {
                foreach (var appointment in schedule.Appointments.ToList())
                {
                    var notification = new Notification
                    {
                        Title = "Clinic Deleted",
                        Message = $"Your appointment with Dr. {clinic.Doctor.FullName} " +
                                  $"at {clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                                  $"at {appointment.AppointmentTime} has been cancelled because the clinic was deleted.",
                        Type = SD.Type_Appointment,
                        Status = SD.Status_Unreaded,
                        PatientId = appointment.PatientId
                    };

                    await _unitOfWork.Notification.AddAsync(notification);
                }
            }

            var doctorNotification = new Notification
            {
                Title = "Clinic Deleted",
                Message = $"Dear Dr. {clinic.Doctor.FullName}, your clinic \"{clinic.Name}\" " +
                  $"was deleted successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}. " +
                  $"All associated schedules and appointments have been removed.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            _unitOfWork.Clinic.Remove(clinic);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Clinic deleted successfully.", clinicId = id });
        }
    }
}
