using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using static Azure.Core.HttpHeader;

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("Patient/Appointment")]
    public class AppointmentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AppointmentController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllAppointments")]
        public async Task<IActionResult> GetAllAppointments(
            [FromQuery] string searchString = "",
            [FromQuery] string dayOfWeek = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
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

            int pageSize = 6;
            var (appointments, totalItems) =
                await _unitOfWork.Appointment.GetFilteredAppointmentsAsync(
                    patient.Id, null, searchString, null, dayOfWeek, currentSort, currentPage, pageSize,
                    new[] { SD.Status_Pending, SD.Status_Confirmed });

            var appointmentRows = new List<AppointmentRowVM>();
            foreach (var a in appointments)
            {
                appointmentRows.Add(new AppointmentRowVM
                {
                    Appointment = a,
                    ClinicName = a.Schedule.Clinic.Name,
                    AppointmentDay = a.Schedule.DayOfWeek,
                    DepartmentName = a.Schedule.Clinic.Doctor.Department.Name,
                    IsPending = a.Status == SD.Status_Pending,
                    IsPayable = a.Status == SD.Status_Confirmed
                        && a.PaymentMethod == SD.PaymentMethod_Online
                        && (a.Payment == null || a.Payment.Status != SD.PaymentStatus_Completed)
                });
            }

            var vm = new AppointmentListVM
            {
                Appointments = appointmentRows,
                SourceView = "GetAllAppointments",

                SearchString = searchString,

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
                NameSort = currentSort == "name" ? "name_desc" : "name",
                DateSort = currentSort == "date" ? "date_desc" : "date",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Patient/Views/Appointment/GetAllAppointments.cshtml", vm);
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

            string displayImageUrl;
            if (!string.IsNullOrEmpty(appointment.Schedule.Clinic.Doctor.ImageUrl))
            {
                displayImageUrl = appointment.Schedule.Clinic.Doctor.ImageUrl;
            }
            else
            {
                displayImageUrl = appointment.Schedule.Clinic.Doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }
            int age = DateTime.Now.Year - appointment.Schedule.Clinic.Doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < appointment.Schedule.Clinic.Doctor.DateOfBirth.AddYears(age).Date)
            {
                age--;
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

            vm.DoctorDisplayImageUrl = displayImageUrl;
            vm.DoctorName = appointment.Schedule.Clinic.Doctor.FullName;
            vm.DoctorAge = age;
            vm.DepartmentName = appointment.Schedule.Clinic.Doctor.Department.Name;
            vm.DoctorAboutMe = appointment.Schedule.Clinic.Doctor.AboutMe;
            vm.ClinicName = appointment.Schedule.Clinic.Name;
            vm.ClinicAddress = appointment.Schedule.Clinic.Address;
            vm.ClinicPhoneNumber = appointment.Schedule.Clinic.PhoneNumber;
            vm.ConsultationPrice = appointment.Schedule.Clinic.ConsultationPrice;
            vm.ScheduleDayOfWeek = appointment.Schedule.DayOfWeek;
            vm.AppointmentDuration = appointment.Schedule.AppointmentDuration;
            vm.ScheduleNotes = string.IsNullOrEmpty(appointment.Schedule.Notes) ? "No notes added" : appointment.Schedule.Notes;
            vm.PatientDisplayImageUrl = patientDisplayImageUrl;

            vm.IsPending = appointment.Status == SD.Status_Pending;
            vm.IsPayable = appointment.Status == SD.Status_Confirmed
               && appointment.PaymentMethod == SD.PaymentMethod_Online
               && (appointment.Payment == null || appointment.Payment.Status != SD.PaymentStatus_Completed);
            vm.PaymentStatus = appointment.Payment != null && appointment.Payment.Status == SD.PaymentStatus_Completed
                    ? SD.PaymentStatus_Completed
                    : SD.PaymentStatus_NotPaid;

            return View("Areas/Patient/Views/Appointment/GetAppointment.cshtml", vm);
        }

        [HttpGet("EditAppointment/{id:int}")]
        public async Task<IActionResult> EditAppointment(
            [FromRoute] int id, [FromQuery] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status != SD.Status_Pending)
            {
                TempData["error"] = "Only pending appointments can be edited.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Patient",
                    id = vm.Appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            vm.Appointment = appointment;
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" }
            };
            vm.PaymentMethodList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Online", Text = "Pay Online" },
                new SelectListItem { Value = "Cash", Text = "Pay at Clinic" }
            };
            if (!string.IsNullOrEmpty(appointment.PatientImageUrl))
            {
                vm.PatientDisplayImageUrl = appointment.PatientImageUrl;
            }
            else
            {
                vm.PatientDisplayImageUrl = appointment.PatientGender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }
            vm.HasCustomImage = !string.IsNullOrEmpty(vm.Appointment.PatientImageUrl);

            return View("Areas/Patient/Views/Appointment/EditAppointment.cshtml", vm);
        }

        [HttpPost("EditAppointment/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppointmentConfirmed([FromForm] AppointmentVM vm)
        {
            if (vm.Appointment.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                string extension = Path.GetExtension(vm.Appointment.ImageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    ModelState.AddModelError("Appointment.ImageFile", "Only jpg, jpeg, png images are allowed");

                if (vm.Appointment.ImageFile.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("Appointment.ImageFile", "Image size must be less than 2MB");
            }

            if (!ModelState.IsValid)
            {
                vm.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" }
                };
                vm.PaymentMethodList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Online", Text = "Pay Online" },
                    new SelectListItem { Value = "Cash", Text = "Pay at Clinic" }
                };

                return View("Areas/Patient/Views/Appointment/EditAppointment.cshtml", vm);
            }

            if (vm.Appointment.ImageFile != null && vm.Appointment.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "appointments");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                if (!string.IsNullOrEmpty(vm.Appointment.PatientImageUrl))
                {
                    string oldImagePath = Path.Combine(wwwRootPath, vm.Appointment.PatientImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString("N");
                string extension = Path.GetExtension(vm.Appointment.ImageFile.FileName);
                string fullPath = Path.Combine(uploadPath, fileName + extension);

                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.Appointment.ImageFile.CopyToAsync(fileStream);
                }

                vm.Appointment.PatientImageUrl = "/images/appointments/" + fileName + extension;
            }

            await _unitOfWork.Appointment.UpdateAsync(vm.Appointment);
            await _unitOfWork.SaveAsync();

            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == vm.Appointment.Id,
                includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound();
            }

            var notification = new Notification
            {
                Title = "Appointment Updated",
                Message = $"Your appointment on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} has been updated successfully.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Appointment updated successfully.";
            return RedirectToAction(vm.SourceView, "Appointment", new
            {
                area = "Patient",
                id = vm.Appointment.Id,
                searchString = vm.SearchString,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpPost("DeletePatientImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatientImage([FromForm] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(a => a.Id == vm.Appointment.Id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status != SD.Status_Pending)
            {
                TempData["error"] = "Only pending appointments can have their images deleted.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Patient",
                    id = vm.Appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            if (!string.IsNullOrEmpty(appointment.PatientImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string oldImagePath = Path.Combine(wwwRootPath, appointment.PatientImageUrl.TrimStart('/'));

                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                appointment.PatientImageUrl = null;
                await _unitOfWork.Appointment.UpdateAsync(appointment);
                await _unitOfWork.SaveAsync();
                TempData["success"] = "Image deleted successfully.";
            }

            return RedirectToAction("EditAppointment", "Appointment", new
            {
                area = "Patient",
                id = vm.Appointment.Id,
                sourceView = vm.SourceView,
                searchString = vm.SearchString,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("CancelAppointment/{id:int}")]
        public async Task<IActionResult> CancelAppointment(
            [FromRoute] int id, [FromQuery] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id, includeProperties: "Schedule.Clinic.Doctor.Department,Patient");
            if (appointment == null)
            {
                return NotFound();
            }

            if (appointment.Status != SD.Status_Pending)
            {
                TempData["error"] = "Only pending appointments can be cancelled.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Patient",
                    id = vm.Appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            string doctorDisplayImageUrl;
            if (!string.IsNullOrEmpty(appointment.Schedule.Clinic.Doctor.ImageUrl))
            {
                doctorDisplayImageUrl = appointment.Schedule.Clinic.Doctor.ImageUrl;
            }
            else
            {
                doctorDisplayImageUrl = appointment.Schedule.Clinic.Doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }
            int age = DateTime.Now.Year - appointment.Schedule.Clinic.Doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < appointment.Schedule.Clinic.Doctor.DateOfBirth.AddYears(age).Date)
            {
                age--;
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

            vm.DoctorDisplayImageUrl = doctorDisplayImageUrl;
            vm.DoctorName = appointment.Schedule.Clinic.Doctor.FullName;
            vm.DoctorAge = age;
            vm.DepartmentName = appointment.Schedule.Clinic.Doctor.Department.Name;
            vm.DoctorAboutMe = appointment.Schedule.Clinic.Doctor.AboutMe;
            vm.ClinicName = appointment.Schedule.Clinic.Name;
            vm.ClinicAddress = appointment.Schedule.Clinic.Address;
            vm.ClinicPhoneNumber = appointment.Schedule.Clinic.PhoneNumber;
            vm.ConsultationPrice = appointment.Schedule.Clinic.ConsultationPrice;
            vm.ScheduleDayOfWeek = appointment.Schedule.DayOfWeek;
            vm.AppointmentDuration = appointment.Schedule.AppointmentDuration;
            vm.ScheduleNotes = string.IsNullOrEmpty(appointment.Schedule.Notes) ? "No notes added" : appointment.Schedule.Notes;
            vm.PatientDisplayImageUrl = patientDisplayImageUrl;

            return View("Areas/Patient/Views/Appointment/CancelAppointment.cshtml", vm);
        }

        [HttpPost("CancelAppointment/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointmentConfirmed([FromForm] AppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == vm.Appointment.Id, includeProperties: "Schedule.Clinic.Doctor");

            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = SD.Status_Cancelled;

            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Cancelled",
                Message = $"Your appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime} has been cancelled.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId,
            };

            await _unitOfWork.Notification.AddAsync(notification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Appointment canceled successfully.";
            return RedirectToAction("GetAllAppointments", "Appointment", new
            {
                area = "Patient",
                id = vm.Appointment.Id,
                searchString = vm.SearchString,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("PayAppointmentByCard/{id:int}")]
        public async Task<IActionResult> PayAppointmentByCard([FromRoute] int id, [FromQuery] PayAppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == id,
                includeProperties: "Schedule.Clinic.Doctor,Patient");

            if (appointment == null) return NotFound();

            if (appointment.Status != SD.Status_Confirmed)
            {
                TempData["error"] = "Only confirmed appointments can be paid.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Patient",
                    id = appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            var existingPayment = await _unitOfWork.Payment.GetFirstOrDefaultAsync(
                p => p.AppointmentId == appointment.Id && p.Status == SD.PaymentStatus_Completed);

            if (existingPayment != null)
            {
                TempData["error"] = "This appointment has already been paid. You cannot pay again.";
                return RedirectToAction(vm.SourceView, "Appointment", new
                {
                    area = "Patient",
                    id = appointment.Id,
                    searchString = vm.SearchString,
                    dayOfWeek = vm.DayOfWeek,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            vm.AppointmentId = appointment.Id;
            vm.PatientName = appointment.PatientName;
            vm.DoctorName = appointment.Schedule.Clinic.Doctor.FullName;
            vm.ClinicName = appointment.Schedule.Clinic.Name;
            vm.ConsultationPrice = appointment.Schedule.Clinic.ConsultationPrice;
            vm.AppointmentDate = appointment.AppointmentDate;
            vm.AppointmentTime = appointment.AppointmentTime;

            return View("Areas/Patient/Views/Appointment/PayAppointmentByCard.cshtml", vm);
        }

        [HttpPost("PayAppointmentByCard/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAppointmentByCardConfirmed(PayAppointmentVM vm)
        {
            var appointment = await _unitOfWork.Appointment.GetFirstOrDefaultAsync(
                a => a.Id == vm.AppointmentId,
                includeProperties: "Schedule.Clinic.Doctor,Patient");

            if (appointment == null)
            {
                return NotFound();
            }

            var result = await _unitOfWork.Payment.ProcessCardPaymentAsync(
                appointment.Id,
                appointment.Schedule.Clinic.ConsultationPrice,
                vm.CardNumber,
                vm.ExpiryMonth,
                vm.ExpiryYear,
                vm.CVV
            );

            var payment = new Payment
            {
                AppointmentId = appointment.Id,
                Amount = appointment.Schedule.Clinic.ConsultationPrice,
                Method = SD.PaymentMethod_Card,
                TransactionId = result.TransactionId,
                Status = result.Success ? SD.PaymentStatus_Completed : SD.PaymentStatus_Failed,
                PaidAt = result.Success ? DateTime.UtcNow : (DateTime?)null
            };

            await _unitOfWork.Payment.AddAsync(payment);
            appointment.Status = result.Success ? SD.Status_Confirmed : SD.Status_Pending;
            await _unitOfWork.Appointment.UpdateAsync(appointment);
            await _unitOfWork.SaveAsync();

            var paymentNotification = new Notification
            {
                Title = result.Success ? "Payment Successful" : "Payment Failed",
                Message = result.Success
            ? $"Your payment for the appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
              $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
              $"at {appointment.AppointmentTime} has been completed successfully."
            : $"Your payment attempt for the appointment with Dr. {appointment.Schedule.Clinic.Doctor.FullName} " +
              $"at {appointment.Schedule.Clinic.Name} on {appointment.AppointmentDate:dddd, dd MMM yyyy} " +
              $"at {appointment.AppointmentTime} has failed. Please try again.",
                Type = SD.Type_Payment,
                Status = SD.Status_Unreaded,
                PatientId = appointment.PatientId
            };

            await _unitOfWork.Notification.AddAsync(paymentNotification);

            var doctorNotification = new Notification
            {
                Title = result.Success ? "Patient Payment Completed" : "Patient Payment Failed",
                Message = result.Success
            ? $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, patient {appointment.Patient.FullName} " +
              $"has successfully paid for their appointment at clinic \"{appointment.Schedule.Clinic.Name}\" " +
              $"scheduled for {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime}."
            : $"Dear Dr. {appointment.Schedule.Clinic.Doctor.FullName}, patient {appointment.Patient.FullName} " +
              $"attempted to pay for their appointment at clinic \"{appointment.Schedule.Clinic.Name}\" " +
              $"scheduled for {appointment.AppointmentDate:dddd, dd MMM yyyy} at {appointment.AppointmentTime}, but the payment failed.",
                Type = SD.Type_Payment,
                Status = SD.Status_Unreaded,
                DoctorId = appointment.Schedule.Clinic.Doctor.Id,
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData[result.Success ? "success" : "error"] =
                result.Success ? "Payment completed successfully." : "Payment failed. Please try again.";

            return RedirectToAction(vm.SourceView, "Appointment", new
            {
                area = "Patient",
                id = appointment.Id,
                searchString = vm.SearchString,
                dayOfWeek = vm.DayOfWeek,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
