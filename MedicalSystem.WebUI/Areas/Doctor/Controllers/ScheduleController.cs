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
    [Route("Doctor/Schedule")]
    public class ScheduleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        public ScheduleController(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
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
                return NotFound();
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound();
            }

            int pageSize = 6;
            var (schedules, totalItems) =
                await _unitOfWork.Schedule.GetFilteredSchedulesAsync(
                    doctor.Id, clinicId, dayOfWeek, currentSort, currentPage, pageSize);

            var scheduleRows = new List<ScheduleRowVM>();
            foreach (var s in schedules)
            {
                scheduleRows.Add(new ScheduleRowVM
                {
                    Schedule = s,
                    ClinicName = s.Clinic.Name
                });
            }

            var vm = new ScheduleListVM
            {
                Schedules = scheduleRows,
                SourceView = "GetAllSchedules",

                ClinicId = clinicId,
                ClinicList = (await _unitOfWork.Clinic.GetAllAsync(c => c.DoctorId == doctor.Id)).Select(
                    c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = (clinicId.HasValue && clinicId.Value == c.Id)
                    }),

                DayOfWeek = dayOfWeek,
                DayOfWeekList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Saturday", Text = "Saturday", Selected = dayOfWeek == "Saturday" },
                    new SelectListItem { Value = "Sunday", Text = "Sunday", Selected = dayOfWeek == "Sunday" },
                    new SelectListItem { Value = "Monday", Text = "Monday", Selected = dayOfWeek == "Monday" },
                    new SelectListItem { Value = "Tuesday", Text = "Tuesday", Selected = dayOfWeek == "Tuesday" },
                    new SelectListItem { Value = "Wednesday", Text = "Wednesday", Selected = dayOfWeek == "Wednesday" },
                    new SelectListItem { Value = "Thursday", Text = "Thursday", Selected = dayOfWeek == "Thursday" },
                    new SelectListItem { Value = "Friday", Text = "Friday", Selected = dayOfWeek == "Friday" }
                },

                CurrentSort = currentSort,
                DaySort = currentSort == "day" ? "day_desc" : "day",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Doctor/Views/Schedule/GetAllSchedules.cshtml", vm);
        }

        [HttpGet("GetSchedule/{id:int}")]
        public async Task<IActionResult> GetSchedule(
            [FromRoute] int id, [FromQuery] ScheduleVM vm)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == id, includeProperties: "Clinic");
            if (schedule == null)
            {
                return NotFound();
            }

            vm.Schedule = schedule;
            vm.SourceView = "GetSchedule";
            vm.ClinicName = schedule.Clinic.Name;
            vm.DisplayNotes = string.IsNullOrEmpty(schedule.Notes) ? "No notes added" : schedule.Notes;

            return View("Areas/Doctor/Views/Schedule/GetSchedule.cshtml", vm);
        }

        [HttpGet("CreateSchedule")]
        public async Task<IActionResult> CreateSchedule([FromQuery] ScheduleVM vm)
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

            vm.ClinicList = (await _unitOfWork.Clinic.GetAllAsync(c => c.DoctorId == doctor.Id)).Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                });

            vm.DayOfWeekList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Saturday", Text = "Saturday" },
                new SelectListItem { Value = "Sunday", Text = "Sunday" },
                new SelectListItem { Value = "Monday", Text = "Monday" },
                new SelectListItem { Value = "Tuesday", Text = "Tuesday" },
                new SelectListItem { Value = "Wednesday", Text = "Wednesday" },
                new SelectListItem { Value = "Thursday", Text = "Thursday" },
                new SelectListItem { Value = "Friday", Text = "Friday" }
            };

            return View("Areas/Doctor/Views/Schedule/CreateSchedule.cshtml", vm);
        }

        [HttpPost("CreateSchedule")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateScheduleConfirmed([FromForm] ScheduleVM vm)
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

            bool hasConflict = await _unitOfWork.Schedule.HasTimeConflictAsync(
                doctor.Id,
                vm.Schedule.DayOfWeek,
                vm.Schedule.StartTime,
                vm.Schedule.EndTime
            );

            if (hasConflict)
            {
                ModelState.AddModelError("Schedule.DayOfWeek", "A schedule with overlapping time already exists for this day.");
            }

            if (!ModelState.IsValid)
            {
                vm.ClinicList = (await _unitOfWork.Clinic.GetAllAsync(c => c.DoctorId == doctor.Id)).Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                });

                vm.DayOfWeekList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Saturday", Text = "Saturday" },
                    new SelectListItem { Value = "Sunday", Text = "Sunday" },
                    new SelectListItem { Value = "Monday", Text = "Monday" },
                    new SelectListItem { Value = "Tuesday", Text = "Tuesday" },
                    new SelectListItem { Value = "Wednesday", Text = "Wednesday" },
                    new SelectListItem { Value = "Thursday", Text = "Thursday" },
                    new SelectListItem { Value = "Friday", Text = "Friday" }
                };

                return View("Areas/Doctor/Views/Schedule/CreateSchedule.cshtml", vm);
            }

            var totalMinutes = (vm.Schedule.EndTime - vm.Schedule.StartTime).TotalMinutes;
            vm.Schedule.MaxPatientsPerDay = (int)(totalMinutes / vm.Schedule.AppointmentDuration);

            await _unitOfWork.Schedule.AddAsync(vm.Schedule);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Schedule Created",
                Message = $"Dear Dr. {doctor.FullName}, your new schedule for {vm.Schedule.DayOfWeek} " +
                    $"from {vm.Schedule.StartTime.ToString(@"hh\:mm")} to {vm.Schedule.EndTime.ToString(@"hh\:mm")} " +
                    $"was created successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Schedule created successfully.";
            return RedirectToAction("GetAllSchedules", "Schedule", new { area = "Doctor" });
        }

        [HttpGet("EditSchedule/{id:int}")]
        public async Task<IActionResult> EditSchedule(
            [FromRoute] int id, [FromQuery] ScheduleVM vm)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == id);
            if (schedule == null)
            {
                return NotFound();
            }

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

            vm.Schedule = schedule;
            vm.ClinicList = (await _unitOfWork.Clinic.GetAllAsync(c => c.DoctorId == doctor.Id)).Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                });

            vm.DayOfWeekList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Saturday", Text = "Saturday" },
                new SelectListItem { Value = "Sunday", Text = "Sunday" },
                new SelectListItem { Value = "Monday", Text = "Monday" },
                new SelectListItem { Value = "Tuesday", Text = "Tuesday" },
                new SelectListItem { Value = "Wednesday", Text = "Wednesday" },
                new SelectListItem { Value = "Thursday", Text = "Thursday" },
                new SelectListItem { Value = "Friday", Text = "Friday" }
            };

            return View("Areas/Doctor/Views/Schedule/EditSchedule.cshtml", vm);
        }

        [HttpPost("EditSchedule/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditScheduleConfirmed([FromForm] ScheduleVM vm)
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

            bool hasConflict = await _unitOfWork.Schedule.HasTimeConflictAsync(
                doctor.Id,
                vm.Schedule.DayOfWeek,
                vm.Schedule.StartTime,
                vm.Schedule.EndTime,
                vm.Schedule.Id
            );

            if (hasConflict)
            {
                ModelState.AddModelError("Schedule.DayOfWeek", "Another schedule with overlapping time already exists for this day.");
            }

            if (!ModelState.IsValid)
            {
                vm.ClinicList = (await _unitOfWork.Clinic.GetAllAsync(c => c.DoctorId == doctor.Id)).Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                });

                vm.DayOfWeekList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Saturday", Text = "Saturday" },
                    new SelectListItem { Value = "Sunday", Text = "Sunday" },
                    new SelectListItem { Value = "Monday", Text = "Monday" },
                    new SelectListItem { Value = "Tuesday", Text = "Tuesday" },
                    new SelectListItem { Value = "Wednesday", Text = "Wednesday" },
                    new SelectListItem { Value = "Thursday", Text = "Thursday" },
                    new SelectListItem { Value = "Friday", Text = "Friday" }
                };

                return View("Areas/Doctor/Views/Schedule/EditSchedule.cshtml", vm);
            }

            var totalMinutes = (vm.Schedule.EndTime - vm.Schedule.StartTime).TotalMinutes;
            vm.Schedule.MaxPatientsPerDay = (int)(totalMinutes / vm.Schedule.AppointmentDuration);

            await _unitOfWork.Schedule.UpdateAsync(vm.Schedule);
            await _unitOfWork.SaveAsync();

            var doctorNotification = new Notification
            {
                Title = "Schedule Updated",
                Message = $"Dear Dr. {doctor.FullName}, your schedule for {vm.Schedule.DayOfWeek} " +
                    $"from {DateTime.Today.Add(vm.Schedule.StartTime).ToString("hh:mm tt")} " +
                    $"to {DateTime.Today.Add(vm.Schedule.EndTime).ToString("hh:mm tt")} " +
                    $"was updated successfully on {DateTime.UtcNow:dddd, dd MMM yyyy}.",
                Type = SD.Type_System,
                Status = SD.Status_Unreaded,
                DoctorId = doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Schedule updated successfully.";
            return RedirectToAction(vm.SourceView, "Schedule", new
            {
                area = "Doctor",
                id = vm.Schedule.Id,
                clinicId = vm.ClinicId,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteSchedule/{id:int}")]
        public async Task<IActionResult> DeleteSchedule([FromRoute] int id, [FromQuery] ScheduleVM vm)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == id, includeProperties: "Clinic");
            if (schedule == null)
            {
                return NotFound();
            }

            vm.Schedule = schedule;
            vm.ClinicName = schedule.Clinic.Name;
            vm.DisplayNotes = string.IsNullOrEmpty(schedule.Notes) ? "No notes added" : schedule.Notes;

            return View("Areas/Doctor/Views/Schedule/DeleteSchedule.cshtml", vm);
        }

        [HttpPost("DeleteSchedule/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteScheduleConfirmed([FromForm] ScheduleVM vm)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == vm.Schedule.Id, includeProperties: "Clinic.Doctor,Appointments");
            if (schedule == null)
            {
                return NotFound();
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

            TempData["success"] = "Schedule deleted successfully.";
            return RedirectToAction("GetAllSchedules", "Schedule", new
            {
                area = "Doctor",
                clinicId = vm.ClinicId,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
