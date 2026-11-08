using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicalSystem.WebUI.Areas.Doctor.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Doctor)]
    [Area("Doctor")]
    [Route("Doctor/Appointment")]
    public class AppointmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("GetAllAppointments")]
        public async Task<IActionResult> GetAllAppointments(
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
            var (appointments, totalItems) =
                await _unitOfWork.Appointment.GetFilteredAppointmentsAsync(
                    null, doctor.Id, "", clinicId, dayOfWeek, currentSort, currentPage, pageSize,
                    new[] { SD.Status_Pending, SD.Status_Confirmed });

            var appointmentRows = new List<AppointmentRowVM>();
            foreach (var a in appointments)
            {
                appointmentRows.Add(new AppointmentRowVM
                {
                    Appointment = a,
                    ClinicName = a.Schedule.Clinic.Name,
                    AppointmentDay = a.Schedule.DayOfWeek,
                    IsPending = a.Status == SD.Status_Pending,
                    CanBeCompleted = a.Status == SD.Status_Confirmed && DateTime.Now >= a.AppointmentDate.Add(a.AppointmentTime)
                });
            }

            var vm = new AppointmentListVM
            {
                Appointments = appointmentRows,
                SourceView = "GetAllAppointments",

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
                PatientSort = currentSort == "patient" ? "patient_desc" : "patient",
                DateSort = currentSort == "date" ? "date_desc" : "date",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Doctor/Views/Appointment/GetAllAppointments.cshtml", vm);
        }

        [HttpGet("GetAppointment/{id:int}")]
        public async Task<IActionResult> GetAppointment(
            [FromRoute] int id, [FromQuery] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "Schedule.Clinic.Doctor.Department,Patient,Payment");
            if (appointment == null)
            {
                return NotFound();
            }

            string patientDisplayImageUrl;
            if (!string.IsNullOrEmpty(appointment.PatientImageUrl))
            {
                patientDisplayImageUrl = appointment.PatientImageUrl;
            }
            else
            {
                patientDisplayImageUrl = appointment.PatientGender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }

            vm.Appointment = appointment;
            vm.SourceView = "GetAppointment";

            vm.PatientDisplayImageUrl = patientDisplayImageUrl;
            vm.ClinicName = appointment.Schedule.Clinic.Name;
            vm.ClinicAddress = appointment.Schedule.Clinic.Address;
            vm.ClinicPhoneNumber = appointment.Schedule.Clinic.PhoneNumber;
            vm.ConsultationPrice = appointment.Schedule.Clinic.ConsultationPrice;
            vm.ScheduleDayOfWeek = appointment.Schedule.DayOfWeek;
            vm.AppointmentDuration = appointment.Schedule.AppointmentDuration;
            vm.ScheduleNotes = string.IsNullOrEmpty(appointment.Schedule.Notes) ? "No notes added" : appointment.Schedule.Notes;

            vm.IsPending = appointment.Status == SD.Status_Pending;
            vm.CanBeCompleted = appointment.Status == SD.Status_Confirmed && DateTime.Now >= appointment.AppointmentDate.Add(appointment.AppointmentTime);
            vm.PaymentStatus = appointment.Payment != null && appointment.Payment.Status == SD.PaymentStatus_Completed
                    ? SD.PaymentStatus_Completed
                    : SD.PaymentStatus_NotPaid;

            return View("Areas/Doctor/Views/Appointment/GetAppointment.cshtml", vm);
        }

        [HttpPost("CancelAppointment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment([FromForm] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == vm.Appointment.Id, includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status != SD.Status_Pending)
            {
                TempData["error"] = "Only pending appointments can be canceled.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Doctor",
                    id = appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            appointment.Status = SD.Status_Cancelled;

            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Cancelled",
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
                  $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointment.AppointmentTime} has been cancelled by the doctor.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "Appointment Cancelled",
                Message = $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, you cancelled the appointment " +
                  $"with patient ID {appointment.PatientId} at clinic \"{appointment.Schedule.Clinic.Name}\" " +
                  $"scheduled for {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Appointment canceled successfully.";
            return RedirectToAction("GetAllAppointments", "Appointment", new
            {
                area = "Doctor",
                id = vm.Appointment.Id,
                clinicId = vm.ClinicId,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("ConfirmAppointment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment([FromForm] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == vm.Appointment.Id, includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status != SD.Status_Pending)
            {
                TempData["error"] = "Only pending appointments can be confirmed.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Doctor",
                    id = appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            appointment.Status = SD.Status_Confirmed;

            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Confirmed",
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
                  $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointment.AppointmentTime} has been confirmed.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "Appointment Confirmed",
                Message = $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, you confirmed the appointment " +
                  $"with patient ID {appointment.PatientId} at clinic \"{appointment.Schedule.Clinic.Name}\" " +
                  $"scheduled for {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            if (appointment.PaymentMethod == SD.PaymentMethod_Online)
            {
                var paymentNotification = new Notification
                {
                    Title = "Payment Required",
                    Message = "Please complete your online payment to secure your appointment.",
                    Type = SD.Type_Payment,
                    Status = SD.Status_Unreaded,
                    PatientId = appointment.PatientId
                };
                await _unitOfWork.Notification.AddAsync(paymentNotification);
            }
            else if (appointment.PaymentMethod == SD.PaymentMethod_Cash)
            {
                var paymentNotification = new Notification
                {
                    Title = "Pay at Clinic",
                    Message = "Please pay at the clinic reception before your consultation.",
                    Type = SD.Type_Payment,
                    Status = SD.Status_Unreaded,
                    PatientId = appointment.PatientId
                };
                await _unitOfWork.Notification.AddAsync(paymentNotification);
            }

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Appointment confirmed successfully.";
            return RedirectToAction(vm.SourceView, "Appointment", new
            {
                area = "Doctor",
                id = vm.Appointment.Id,
                clinicId = vm.ClinicId,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("CompleteAppointment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment([FromForm] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == vm.Appointment.Id, includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status != SD.Status_Confirmed)
            {
                TempData["error"] = "Only confirmed appointments can be completed.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Doctor",
                    id = appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            appointment.Status = SD.Status_Completed;

            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Completed",
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
                  $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointment.AppointmentTime} has been marked as completed.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "Appointment Completed",
                Message = $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, you marked the appointment " +
                          $"with patient ID {appointment.PatientId} at clinic \"{appointment.Schedule.Clinic.Name}\" " +
                          $"on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} as completed.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id
            };

            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Appointment completed successfully.";
            return RedirectToAction("GetAllAppointments", "Appointment", new
            {
                area = "Doctor",
                id = vm.Appointment.Id,
                clinicId = vm.ClinicId,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
