using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Schedule;
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
    public class ScheduleApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public ScheduleApiController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetAllSchedules")]
        public async Task<IActionResult> GetAllSchedules(
        [FromQuery] int? clinicId = null,
        [FromQuery] string dayOfWeek = "",
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
            var (schedules, totalItems) =
                await _unitOfWork.Schedule.GetFilteredSchedulesAsync(
                    doctor.Id, clinicId, dayOfWeek, currentSort, currentPage, pageSize);

            var dtoList = schedules.Select(s => new GetAllSchedulesDto
            {
                Id = s.Id,
                ClinicName = s.Clinic.Name,
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                CreatedAt = s.CreatedAt
            }).ToList();

            var response = new
            {
                Schedules = dtoList,
                ClinicId = clinicId,
                DayOfWeek = dayOfWeek,
                CurrentSort = currentSort,
                DaySort = currentSort == "day" ? "day_desc" : "day",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(response);
        }

        [HttpGet("GetSchedule/{id:int}")]
        public async Task<IActionResult> GetSchedule([FromRoute] int id)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == id,
                includeProperties: "Clinic"
            );

            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }

            var dto = new GetScheduleDto
            {
                Id = schedule.Id,
                ClinicName = schedule.Clinic.Name,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                AppointmentDuration = schedule.AppointmentDuration,
                MaxPatientsPerDay = schedule.MaxPatientsPerDay,
                Notes = string.IsNullOrEmpty(schedule.Notes) ? "No notes added" : schedule.Notes,
                CreatedAt = schedule.CreatedAt
            };

            return Ok(dto);
        }

        [HttpPost("CreateSchedule")]
        public async Task<IActionResult> CreateSchedule([FromForm] ScheduleDto dto)
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

            bool hasConflict = await _unitOfWork.Schedule.HasTimeConflictAsync(
                doctor.Id,
                dto.DayOfWeek,
                dto.StartTime,
                dto.EndTime
            );

            if (hasConflict)
            {
                return BadRequest(new { message = "A schedule with overlapping time already exists for this day." });
            }

            var totalMinutes = (dto.EndTime - dto.StartTime).TotalMinutes;
            int maxPatientsPerDay = (int)(totalMinutes / dto.AppointmentDuration);

            var schedule = new Schedule
            {
                ClinicId = dto.ClinicId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                AppointmentDuration = dto.AppointmentDuration,
                MaxPatientsPerDay = maxPatientsPerDay,
                Notes = string.IsNullOrEmpty(dto.Notes) ? null : dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Schedule.AddAsync(schedule);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Schedule Created",
                Message = $"Dear Dr. {doctor.FullName}, your new schedule for {schedule.DayOfWeek} " +
                    $"from {schedule.StartTime.ToString(@"hh\:mm")} to {schedule.EndTime.ToString(@"hh\:mm")} " +
                    $"was created successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Schedule created successfully.",
            });
        }

        [HttpPut("EditSchedule/{id:int}")]
        public async Task<IActionResult> EditSchedule([FromRoute] int id, [FromForm] ScheduleDto dto)
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

            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(s => s.Id == id);
            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }

            bool hasConflict = await _unitOfWork.Schedule.HasTimeConflictAsync(
                doctor.Id,
                dto.DayOfWeek,
                dto.StartTime,
                dto.EndTime,
                id
            );

            if (hasConflict)
            {
                return BadRequest(new { message = "Another schedule with overlapping time already exists for this day." });
            }

            var totalMinutes = (dto.EndTime - dto.StartTime).TotalMinutes;
            int maxPatientsPerDay = (int)(totalMinutes / dto.AppointmentDuration);

            schedule.ClinicId = dto.ClinicId;
            schedule.DayOfWeek = dto.DayOfWeek;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.AppointmentDuration = dto.AppointmentDuration;
            schedule.MaxPatientsPerDay = maxPatientsPerDay;
            schedule.Notes = string.IsNullOrEmpty(dto.Notes) ? null : dto.Notes;

            await _unitOfWork.Schedule.UpdateAsync(schedule);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Schedule Updated",
                Message = $"Dear Dr. {doctor.FullName}, your schedule for {schedule.DayOfWeek} " +
                    $"from {DateTime.Today.Add(schedule.StartTime).ToString("hh:mm tt")} " +
                    $"to {DateTime.Today.Add(schedule.EndTime).ToString("hh:mm tt")} " +
                    $"was updated successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                message = "Schedule updated successfully.",
            });
        }

        [HttpDelete("DeleteSchedule/{id:int}")]
        public async Task<IActionResult> DeleteSchedule([FromRoute] int id)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(s => s.Id == id, includeProperties: "Clinic.Doctor,Appointments");
            if (schedule == null)
            {
                return NotFound(new { message = "Schedule not found." });
            }

            foreach (var appointment in schedule.Appointments.ToList())
            {
                var notification = new Notification
                {
                    Title = "Schedule Cancelled",
                    Message = $"Your appointment with Dr. {schedule.Clinic.Doctor.FullName} " +
                              $"at {schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                              $"at {appointment.AppointmentTime} has been cancelled because the schedule was deleted.",
                    Type = SD.Type_Appointment,
                    Status = SD.Status_Unreaded,
                    PatientId = appointment.PatientId
                };

                await _unitOfWork.Notification.AddAsync(notification);
            }

            var doctorNotification = new Notification
            {
                Title = "Schedule Deleted",
                Message = $"Dear Dr. {schedule.Clinic.Doctor.FullName}, your schedule for {schedule.DayOfWeek} " +
                  $"from {DateTime.Today.Add(schedule.StartTime).ToString("hh:mm tt")} " +
                  $"to {DateTime.Today.Add(schedule.EndTime).ToString("hh:mm tt")} " +
                  $"at clinic \"{schedule.Clinic.Name}\" was deleted successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = schedule.Clinic.Doctor.Id,
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            _unitOfWork.Schedule.Remove(schedule);
            await _unitOfWork.SaveAsync();

            return Ok(new { message = "Schedule deleted successfully." });
        }
    }
}
