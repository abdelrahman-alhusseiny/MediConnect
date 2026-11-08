using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using Stripe;
using System.Numerics;

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("Patient/Booking")]
    public class BookingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BookingController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("GetAllDoctors")]
        public async Task<IActionResult> GetAllDoctors(
            [FromQuery] string searchString = "",
            [FromQuery] int? departmentId = null,
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (doctors, totalItems) =
                await _unitOfWork.Doctor.GetFilteredDoctorsAsync(
                    searchString, "", departmentId, currentSort, currentPage, pageSize);

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
                int age = DateTime.Now.Year - d.DateOfBirth.Year;
                if (DateTime.Now.Date < d.DateOfBirth.AddYears(age).Date)
                {
                    age--;
                }
                doctorRows.Add(new DoctorRowVM
                {
                    Doctor = d,
                    DisplayImageUrl = displayImageUrl,
                    DepartmentName = d.Department.Name,
                    Age = age,
                    AverageRating = await _unitOfWork.DoctorReview.GetAverageRatingAsync(d.Id)
                });
            }

            var vm = new DoctorListVM
            {
                Doctors = doctorRows,
                SourceView = "GetAllDoctors",

                SearchString = searchString,

                DepartmentId = departmentId,
                DepartmentList = (await _unitOfWork.Department.GetAllAsync()).Select(
                d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name,
                    Selected = d.Id == departmentId
                }),

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                RateSort = currentSort == "rate" ? "rate_desc" : "rate",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Patient/Views/Booking/GetAllDoctors.cshtml", vm);
        }

        [HttpGet("GetDoctor/{doctorId:int}")]
        public async Task<IActionResult> GetDoctor(
            [FromRoute] int doctorId, [FromQuery] DoctorVM vm)
        {
            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.Id == doctorId, includeProperties: "AppUser,Department");
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
            vm.Age = DateTime.Now.Year - doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < doctor.DateOfBirth.AddYears(vm.Age ?? 0).Date)
            {
                vm.Age--;
            }
            vm.DepartmentName = doctor.Department.Name;
            vm.DepartmentDescription = doctor.Department.Description;
            vm.AverageRating = await _unitOfWork.DoctorReview.GetAverageRatingAsync(doctor.Id);

            return View("Areas/Patient/Views/Booking/GetDoctor.cshtml", vm);
        }

        [HttpGet("GetDoctorReviews")]
        public async Task<IActionResult> GetDoctorReviews(
            [FromQuery] int doctorId,
            [FromQuery] string sourceView = "",
            [FromQuery] string searchString = "",
            [FromQuery] int? departmentId = null,
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            var doctorReviews = await _unitOfWork.DoctorReview.GetFilteredDoctorReviewsAsync(doctorId);

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

            var doctorReviewRows = new List<DoctorReviewRowVM>();
            foreach (var r in doctorReviews)
            {
                string displayImageUrl;
                if (!string.IsNullOrEmpty(r.Patient.ImageUrl))
                {
                    displayImageUrl = r.Patient.ImageUrl;
                }
                else
                {
                    displayImageUrl = r.Patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
                }

                doctorReviewRows.Add(new DoctorReviewRowVM
                {
                    DoctorReview = r,
                    PatientImageUrl = displayImageUrl,
                    PatientName = r.Patient.FullName,
                });
            }

            var existingReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(
                    r => r.DoctorId == doctorId && r.PatientId == patient.Id);

            var vm = new DoctorReviewListVM
            {
                DoctorReviews = doctorReviewRows,
                SourceView = sourceView,
                DoctorId = doctorId,
                SearchString = searchString,
                DepartmentId = departmentId,
                CurrentSort = currentSort,
                CurrentPage = currentPage,
                HasPatientReviewed = existingReview != null
            };

            return View("Areas/Patient/Views/Booking/GetDoctorReviews.cshtml", vm);
        }

        [HttpGet("CreateDoctorReview/{doctorId:int}")]
        public async Task<IActionResult> CreateDoctorReview([FromRoute] int doctorId, [FromQuery] DoctorReviewVM vm)
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

            var existingReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(
              r => r.DoctorId == doctorId && r.PatientId == patient.Id);

            if (existingReview != null)
            {
                TempData["error"] = "You have already submitted a review for this doctor.";
                return RedirectToAction("GetDoctorReviews", "Appointment", new
                {
                    area = "Patient",
                    doctorId = vm.DoctorId,
                    sourceView = vm.SourceView,
                    searchString = vm.SearchString,
                    departmentId = vm.DepartmentId,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            vm.DoctorId = doctorId;
            vm.RatingList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "★☆☆☆☆ (1)" },
                new SelectListItem { Value = "2", Text = "★★☆☆☆ (2)" },
                new SelectListItem { Value = "3", Text = "★★★☆☆ (3)" },
                new SelectListItem { Value = "4", Text = "★★★★☆ (4)" },
                new SelectListItem { Value = "5", Text = "★★★★★ (5)" }
            };

            return View("Areas/Patient/Views/Booking/CreateDoctorReview.cshtml", vm);
        }

        [HttpPost("CreateDoctorReview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDoctorReviewConfirmed([FromForm] DoctorReviewVM vm)
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

            if (!ModelState.IsValid)
            {
                vm.RatingList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "★☆☆☆☆ (1)" },
                    new SelectListItem { Value = "2", Text = "★★☆☆☆ (2)" },
                    new SelectListItem { Value = "3", Text = "★★★☆☆ (3)" },
                    new SelectListItem { Value = "4", Text = "★★★★☆ (4)" },
                    new SelectListItem { Value = "5", Text = "★★★★★ (5)" }
                };
                return View("Areas/Patient/Views/Booking/CreateDoctorReview.cshtml", vm);
            }

            vm.DoctorReview.DoctorId = vm.DoctorId;
            vm.DoctorReview.PatientId = patient.Id;

            await _unitOfWork.DoctorReview.AddAsync(vm.DoctorReview);
            await _unitOfWork.SaveAsync();

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.Id == vm.DoctorId);
                
            var patientNotification = new Notification
            {
                Title = "Review Submitted",
                Message = $"Thank you! Your review for Dr. {doctor.FullName} has been submitted successfully.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };
            await _unitOfWork.Notification.AddAsync(patientNotification);

            var doctorNotification = new Notification
            {
                Title = "New Review Received",
                Message = $"Dear Dr. {doctor.FullName}, you have received a new review from patient ID {patient.Id}. " +
                          $"Rating: {vm.DoctorReview.Rating} stars. Comment: \"{vm.DoctorReview.Comment}\"",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                DoctorId = vm.DoctorId
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Doctor review created successfully.";
            return RedirectToAction("GetDoctorReviews", "Booking", new
            {
                area = "Patient",
                doctorId = vm.DoctorId,
                sourceView = vm.SourceView,
                searchString = vm.SearchString,
                departmentId = vm.DepartmentId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("EditDoctorReview/{doctorId:int}")]
        public async Task<IActionResult> EditDoctorReview([FromRoute] int doctorId, [FromQuery] DoctorReviewVM vm)
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

            var existingReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(
              r => r.DoctorId == doctorId && r.PatientId == patient.Id);

            if (existingReview == null)
            {
                TempData["error"] = "You have not already submitted a review for this doctor.";
                return RedirectToAction("GetDoctorReviews", "Appointment", new
                {
                    area = "Patient",
                    doctorId = vm.DoctorId,
                    sourceView = vm.SourceView,
                    searchString = vm.SearchString,
                    departmentId = vm.DepartmentId,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            vm.DoctorReview = existingReview;
            vm.DoctorId = doctorId;
            vm.RatingList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "★☆☆☆☆ (1)" },
                new SelectListItem { Value = "2", Text = "★★☆☆☆ (2)" },
                new SelectListItem { Value = "3", Text = "★★★☆☆ (3)" },
                new SelectListItem { Value = "4", Text = "★★★★☆ (4)" },
                new SelectListItem { Value = "5", Text = "★★★★★ (5)" }
            };

            return View("Areas/Patient/Views/Booking/EditDoctorReview.cshtml", vm);
        }

        [HttpPost("EditDoctorReview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctorReviewConfirmed([FromForm] DoctorReviewVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.RatingList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "★☆☆☆☆ (1)" },
                    new SelectListItem { Value = "2", Text = "★★☆☆☆ (2)" },
                    new SelectListItem { Value = "3", Text = "★★★☆☆ (3)" },
                    new SelectListItem { Value = "4", Text = "★★★★☆ (4)" },
                    new SelectListItem { Value = "5", Text = "★★★★★ (5)" }
                };
                return View("Areas/Patient/Views/Booking/EditDoctorReview.cshtml", vm);
            }

            await _unitOfWork.DoctorReview.UpdateAsync(vm.DoctorReview);
            await _unitOfWork.SaveAsync();

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(d => d.Id == vm.DoctorId);

            var patientNotification = new Notification
            {
                Title = "Review Updated",
                Message = $"Your review for Dr. {doctor.FullName} has been updated successfully.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                PatientId = vm.DoctorReview.PatientId
            };
            await _unitOfWork.Notification.AddAsync(patientNotification);

            var doctorNotification = new Notification
            {
                Title = "Review Updated",
                Message = $"Dear Dr. {doctor.FullName}, patient ID {vm.DoctorReview.PatientId} has updated their review. " +
                          $"New Rating: {vm.DoctorReview.Rating} stars. Comment: \"{vm.DoctorReview.Comment}\"",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                DoctorId = vm.DoctorReview.DoctorId
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Doctor review updated successfully.";
            return RedirectToAction("GetDoctorReviews", "Booking", new
            {
                area = "Patient",
                doctorId = vm.DoctorId,
                sourceView = vm.SourceView,
                searchString = vm.SearchString,
                departmentId = vm.DepartmentId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("DeleteDoctorReview/{doctorId:int}")]
        public async Task<IActionResult> DeleteDoctorReview([FromRoute] int doctorId, [FromQuery] DoctorReviewVM vm)
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

            var existingReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(
              r => r.DoctorId == doctorId && r.PatientId == patient.Id, includeProperties: "Patient,Doctor");

            if (existingReview == null)
            {
                TempData["error"] = "You have not already submitted a review for this doctor.";
                return RedirectToAction("GetDoctorReviews", "Appointment", new
                {
                    area = "Patient",
                    doctorId = vm.DoctorId,
                    sourceView = vm.SourceView,
                    searchString = vm.SearchString,
                    departmentId = vm.DepartmentId,
                    currentSort = vm.CurrentSort,
                    currentPage = vm.CurrentPage
                });
            }

            vm.DoctorReview = existingReview;
            vm.DoctorId = doctorId;

            if (!string.IsNullOrEmpty(existingReview.Patient.ImageUrl))
            {
                vm.PatientImageUrl = existingReview.Patient.ImageUrl;
            }
            else
            {
                vm.PatientImageUrl = existingReview.Patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }
            vm.PatientName = existingReview.Patient.FullName;

            return View("Areas/Patient/Views/Booking/DeleteDoctorReview.cshtml", vm);
        }

        [HttpPost("DeleteDoctorReview/{doctorId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctorReviewConfirmed([FromForm] DoctorReviewVM vm)
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

            var doctorReview = await _unitOfWork.DoctorReview.GetFirstOrDefaultAsync(r => r.DoctorId == vm.DoctorId && r.PatientId == patient.Id, includeProperties: "Patient,Doctor");

            if (doctorReview == null)
            {
                return NotFound();
            }

            _unitOfWork.DoctorReview.Remove(doctorReview);
            await _unitOfWork.SaveAsync();

            var patientNotification = new Notification
            {
                Title = "Review Deleted",
                Message = $"Your review for Dr. {doctorReview.Doctor.FullName} has been deleted successfully.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };
            await _unitOfWork.Notification.AddAsync(patientNotification);

            var doctorNotification = new Notification
            {
                Title = "Review Deleted",
                Message = $"Dear Dr. {doctorReview.Doctor.FullName}, patient {patient.FullName} has deleted their review.",
                Type = SD.Type_Review,
                Status = SD.Status_Unreaded,
                DoctorId = doctorReview.DoctorId
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Doctor review deleted successfully.";
            return RedirectToAction("GetDoctorReviews", "Booking", new
            {
                area = "Patient",
                doctorId = vm.DoctorId,
                sourceView = vm.SourceView,
                searchString = vm.SearchString,
                departmentId = vm.DepartmentId,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }

        [HttpGet("GetDoctorClinics/{doctorId:int}")]
        public async Task<IActionResult> GetDoctorClinics(
            [FromRoute] int doctorId,
            [FromQuery] string searchString = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (clinics, totalItems) =
                await _unitOfWork.Clinic.GetFilteredClinicsAsync(
                    doctorId, searchString, currentSort, currentPage, pageSize);

            var clinicRows = new List<ClinicRowVM>();
            foreach (var c in clinics)
            {
                clinicRows.Add(new ClinicRowVM
                {
                    Clinic = c,
                });
            }

            var vm = new ClinicListVM
            {
                DoctorId = doctorId,

                Clinics = clinicRows,

                SearchString = searchString,

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Patient/Views/Booking/GetDoctorClinics.cshtml", vm);
        }

        [HttpGet("GetClinicSchedules/{clinicId:int}")]
        public async Task<IActionResult> GetClinicSchedules(
            [FromRoute] int clinicId,
            [FromQuery] int doctorId,
            [FromQuery] string dayOfWeek = "",
            [FromQuery] string currentSort = "",
            [FromQuery] int currentPage = 1)
        {
            int pageSize = 6;
            var (schedules, totalItems) =
                await _unitOfWork.Schedule.GetFilteredSchedulesAsync(
                    null, clinicId, dayOfWeek, currentSort, currentPage, pageSize);

            var scheduleRows = new List<ScheduleRowVM>();
            foreach (var s in schedules)
            {
                scheduleRows.Add(new ScheduleRowVM
                {
                    Schedule = s,
                    DisplayNotes = string.IsNullOrEmpty(s.Notes) ? "No notes added" : s.Notes
                });
            }

            var vm = new ScheduleListVM
            {
                DoctorId = doctorId,
                ClinicId = clinicId,

                Schedules = scheduleRows,

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

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Patient/Views/Booking/GetClinicSchedules.cshtml", vm);
        }

        [HttpGet("AppointmentSummary/{scheduleId:int}")]
        public async Task<IActionResult> AppointmentSummary(
            [FromRoute] int scheduleId,
            [FromQuery] int clinicId,
            [FromQuery] int doctorId)
        {
            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == scheduleId, includeProperties: "Clinic.Doctor.Department");
            if (schedule == null)
            {
                return NotFound();
            }

            string doctorDisplayImageUrl;
            if (!string.IsNullOrEmpty(schedule.Clinic.Doctor.ImageUrl))
            {
                doctorDisplayImageUrl = schedule.Clinic.Doctor.ImageUrl;
            }
            else
            {
                doctorDisplayImageUrl = schedule.Clinic.Doctor.Gender?.ToLower() == "male" ? "/images/doctors/default-doctor-male.png" : "/images/doctors/default-doctor-female.png";
            }
            int doctorAge = DateTime.Now.Year - schedule.Clinic.Doctor.DateOfBirth.Year;
            if (DateTime.Now.Date < schedule.Clinic.Doctor.DateOfBirth.AddYears(doctorAge).Date)
            {
                doctorAge--;
            }

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound();
            }

            string patientDisplayImageUrl;
            if (!string.IsNullOrEmpty(patient.ImageUrl))
            {
                patientDisplayImageUrl = patient.ImageUrl;
            }
            else
            {
                patientDisplayImageUrl = patient.Gender?.ToLower() == "male" ? "/images/patients/default-patient-male.png" : "/images/patients/default-patient-female.png";
            }
            int patientAge = DateTime.Now.Year - patient.DateOfBirth.Year;
            if (DateTime.Now.Date < patient.DateOfBirth.AddYears(patientAge).Date)
            {
                patientAge--;
            }

            var vm = new AppointmentSummaryVM
            {
                ScheduleId = scheduleId,
                ClinicId = clinicId,
                DoctorId = doctorId,

                DoctorDisplayImageUrl = doctorDisplayImageUrl,
                DoctorName = schedule.Clinic.Doctor.FullName,
                DoctorAge = doctorAge,
                DepartmentName = schedule.Clinic.Doctor.Department.Name,
                DoctorAboutMe = schedule.Clinic.Doctor.AboutMe,

                ClinicName = schedule.Clinic.Name,
                ClinicAddress = schedule.Clinic.Address,
                ClinicPhoneNumber = schedule.Clinic.PhoneNumber,
                ConsultationPrice = schedule.Clinic.ConsultationPrice,

                ScheduleDayOfWeek = schedule.DayOfWeek,
                ScheduleStartTime = schedule.StartTime,
                ScheduleEndTime = schedule.EndTime,
                AppointmentDuration = schedule.AppointmentDuration,
                ScheduleNotes = string.IsNullOrEmpty(schedule.Notes) ? "No notes added" : schedule.Notes,

                PatientDisplayImageUrl = patientDisplayImageUrl,
                PatientName = patient.FullName,
                PatientPhoneNumber = patient.PhoneNumber,
                PatientAddress = patient.Address,
                PatientGender = patient.Gender,
                PatientAge = patientAge
            };

            return View("Areas/Patient/Views/Booking/AppointmentSummary.cshtml", vm);
        }

        [HttpGet("BookAppointment/{scheduleId:int}")]
        public async Task<IActionResult> BookAppointment([FromRoute] int scheduleId, [FromQuery] AppointmentVM vm)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound();
            }

            int patientAge = DateTime.Now.Year - patient.DateOfBirth.Year;
            if (DateTime.Now.Date < patient.DateOfBirth.AddYears(patientAge).Date)
            {
                patientAge--;
            }

            vm.ScheduleId = scheduleId;
            vm.PaymentMethodList = new List<SelectListItem>
            {
                new SelectListItem { Value = SD.PaymentMethod_Online, Text = "Pay Online" },
                new SelectListItem { Value = SD.PaymentMethod_Cash, Text = "Pay at Clinic" }
            };
            vm.Appointment = new Appointment();
            vm.Appointment.PatientName = patient.FullName;
            vm.Appointment.PatientPhoneNumber = patient.PhoneNumber;
            vm.Appointment.PatientGender = patient.Gender;
            vm.Appointment.PatientAge = patientAge;
            vm.Appointment.PatientAddress = patient.Address;
            vm.Appointment.PatientImageUrl = patient.ImageUrl;

            return View("Areas/Patient/Views/Booking/BookAppointment.cshtml", vm);
        }

        [HttpPost("BookAppointment/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointmentConfirmed([FromForm] AppointmentVM vm)
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound();
            }

            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == vm.ScheduleId, includeProperties: "Clinic.Doctor.Department");
            if (schedule == null)
            {
                return NotFound();
            }

            var existingPatientAppointments = await _unitOfWork.Appointment
                .GetAllAsync(a => a.ScheduleId == vm.ScheduleId && a.PatientId == patient.Id);

            DayOfWeek targetDay = schedule.DayOfWeek.ToLower() switch
            {
                "sunday" => DayOfWeek.Sunday,
                "monday" => DayOfWeek.Monday,
                "tuesday" => DayOfWeek.Tuesday,
                "wednesday" => DayOfWeek.Wednesday,
                "thursday" => DayOfWeek.Thursday,
                "friday" => DayOfWeek.Friday,
                "saturday" => DayOfWeek.Saturday,
                _ => throw new ArgumentException($"Invalid day of week: {schedule.DayOfWeek}")
            };

            var today = DateTime.Today;
            var now = DateTime.Now;

            int daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;

            if (daysUntilTarget == 0)
            {
                DateTime scheduleStart = today.Add(schedule.StartTime);

                if (now < scheduleStart)
                {
                    daysUntilTarget = 0;
                }
                else
                {
                    daysUntilTarget = 7;
                }
            }

            int weeksToAdd = existingPatientAppointments.Count();
            daysUntilTarget += (7 * weeksToAdd);

            DateTime appointmentDate = today.AddDays(daysUntilTarget);

            var existingAppointments = await _unitOfWork.Appointment
                    .GetAllAsync(a => a.ScheduleId == vm.ScheduleId
                   && a.AppointmentDate == appointmentDate
                   && a.Status != SD.Status_Cancelled);

            TimeSpan appointmentTime = schedule.StartTime;
            while (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) <= schedule.EndTime)
            {
                bool isTaken = existingAppointments.Any(a => a.AppointmentTime == appointmentTime);
                if (!isTaken)
                {
                    break;
                }
                appointmentTime = appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration));
            }

            if (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) > schedule.EndTime &&
                existingAppointments.Any(a => a.AppointmentTime == appointmentTime))
            {
                ModelState.AddModelError(string.Empty, "No available slots in this schedule.");
            }

            if (!ModelState.IsValid)
            {
                vm.PaymentMethodList = new List<SelectListItem>
                {
                    new SelectListItem { Value = SD.PaymentMethod_Online, Text = "Pay Online" },
                    new SelectListItem { Value = SD.PaymentMethod_Cash, Text = "Pay at Clinic" }
                };

                return View("Areas/Patient/Views/Booking/BookAppointment.cshtml", vm);
            }

            if (!string.IsNullOrEmpty(vm.Appointment.PatientImageUrl))
            {
                var fileName = Path.GetFileName(vm.Appointment.PatientImageUrl);

                var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "patients", fileName);
                var destPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "appointments", fileName);

                var destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Copy(sourcePath, destPath, overwrite: true);
                    vm.Appointment.PatientImageUrl = $"/images/appointments/{fileName}";
                }
            }

            vm.Appointment.AppointmentDate = appointmentDate;
            vm.Appointment.AppointmentTime = appointmentTime;
            vm.Appointment.Status = SD.Status_Pending;

            vm.Appointment.ScheduleId = schedule.Id;
            vm.Appointment.PatientId = patient.Id;

            await _unitOfWork.Appointment.AddAsync(vm.Appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Booked",
                Message = $"Your appointment with Dr. {schedule.Clinic.Doctor.FullName} at {schedule.Clinic.Name} has been booked for {appointmentDate:dddd, dd MMM yyyy} at {appointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "New Appointment Booked",
                Message = $"Dear Dr. {schedule.Clinic.Doctor.FullName}, a new appointment has been booked " +
                  $"in your schedule at clinic \"{schedule.Clinic.Name}\" for {appointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointmentTime} by patient ID {patient.Id}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = schedule.Clinic.Doctor.Id
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Your appointment has been booked successfully.";
            return RedirectToAction("GetAllAppointments", "Appointment", new { area = "Patient", id = vm.Appointment.Id });
        }

        [HttpGet("BookForOther/{scheduleId:int}")]
        public async Task<IActionResult> BookForOther([FromRoute] int scheduleId, [FromQuery] AppointmentVM vm)
        {
            vm.ScheduleId = scheduleId;
            vm.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" }
            };
            vm.PaymentMethodList = new List<SelectListItem>
            {
                new SelectListItem { Value = SD.PaymentMethod_Online, Text = "Pay Online" },
                new SelectListItem { Value = SD.PaymentMethod_Cash, Text = "Pay at Clinic" }
            };

            return View("Areas/Patient/Views/Booking/BookForOther.cshtml", vm);
        }

        [HttpPost("BookForOther/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookForOtherConfirmed([FromForm] AppointmentVM vm)
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

            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound();
            }

            var patient = await _unitOfWork.Patient.GetFirstOrDefaultAsync(p => p.AppUserId == appUser.Id);
            if (patient == null)
            {
                return NotFound();
            }

            var schedule = await _unitOfWork.Schedule.GetFirstOrDefaultAsync(
                s => s.Id == vm.ScheduleId, includeProperties: "Clinic.Doctor.Department");
            if (schedule == null)
            {
                return NotFound();
            }

            var existingPatientAppointments = await _unitOfWork.Appointment
                .GetAllAsync(a => a.ScheduleId == vm.ScheduleId && a.PatientId == patient.Id);

            DayOfWeek targetDay = schedule.DayOfWeek.ToLower() switch
            {
                "sunday" => DayOfWeek.Sunday,
                "monday" => DayOfWeek.Monday,
                "tuesday" => DayOfWeek.Tuesday,
                "wednesday" => DayOfWeek.Wednesday,
                "thursday" => DayOfWeek.Thursday,
                "friday" => DayOfWeek.Friday,
                "saturday" => DayOfWeek.Saturday,
                _ => throw new ArgumentException($"Invalid day of week: {schedule.DayOfWeek}")
            };

            var today = DateTime.Today;
            var now = DateTime.Now;

            int daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;

            if (daysUntilTarget == 0)
            {
                DateTime scheduleStart = today.Add(schedule.StartTime);

                if (now < scheduleStart)
                {
                    daysUntilTarget = 0;
                }
                else
                {
                    daysUntilTarget = 7;
                }
            }

            int weeksToAdd = existingPatientAppointments.Count();
            daysUntilTarget += (7 * weeksToAdd);

            DateTime appointmentDate = today.AddDays(daysUntilTarget);

            var existingAppointments = await _unitOfWork.Appointment
                    .GetAllAsync(a => a.ScheduleId == vm.ScheduleId
                   && a.AppointmentDate == appointmentDate
                   && a.Status != SD.Status_Cancelled);

            TimeSpan appointmentTime = schedule.StartTime;
            while (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) <= schedule.EndTime)
            {
                bool isTaken = existingAppointments.Any(a => a.AppointmentTime == appointmentTime);
                if (!isTaken)
                {
                    break;
                }
                appointmentTime = appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration));
            }

            if (appointmentTime.Add(TimeSpan.FromMinutes(schedule.AppointmentDuration)) > schedule.EndTime &&
                existingAppointments.Any(a => a.AppointmentTime == appointmentTime))
            {
                ModelState.AddModelError(string.Empty, "No available slots in this schedule.");
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
                    new SelectListItem { Value = SD.PaymentMethod_Online, Text = "Pay Online" },
                    new SelectListItem { Value = SD.PaymentMethod_Cash, Text = "Pay at Clinic" }
                };

                return View("Areas/Patient/Views/Booking/BookForOther.cshtml", vm);
            }

            if (vm.Appointment.ImageFile != null && vm.Appointment.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string uploadPath = Path.Combine(wwwRootPath, "images", "appointments");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
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

            vm.Appointment.AppointmentDate = appointmentDate;
            vm.Appointment.AppointmentTime = appointmentTime;
            vm.Appointment.Status = SD.Status_Pending;

            vm.Appointment.ScheduleId = schedule.Id;
            vm.Appointment.PatientId = patient.Id;

            await _unitOfWork.Appointment.AddAsync(vm.Appointment);
            await _unitOfWork.SaveAsync();

            var notification = new Notification
            {
                Title = "Appointment Booked",
                Message = $"Your appointment with Dr. {schedule.Clinic.Doctor.FullName} at {schedule.Clinic.Name} has been booked for {appointmentDate:dddd, dd MMM yyyy} at {appointmentTime}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                PatientId = patient.Id
            };

            await _unitOfWork.Notification.AddAsync(notification);

            var doctorNotification = new Notification
            {
                Title = "New Appointment Booked",
                Message = $"Dear Dr. {schedule.Clinic.Doctor.FullName}, a new appointment has been booked " +
                  $"in your schedule at clinic \"{schedule.Clinic.Name}\" for {appointmentDate:dddd, dd MMM yyyy} " +
                  $"at {appointmentTime} by patient ID {patient.Id}.",
                Type = SD.Type_Appointment,
                Status = SD.Status_Unreaded,
                DoctorId = schedule.Clinic.Doctor.Id
            };
            await _unitOfWork.Notification.AddAsync(doctorNotification);

            await _unitOfWork.SaveAsync();

            TempData["success"] = "Your appointment has been booked successfully.";
            return RedirectToAction("GetAllAppointments", "Appointment", new { area = "Patient", id = vm.Appointment.Id });
        }
    }
}
