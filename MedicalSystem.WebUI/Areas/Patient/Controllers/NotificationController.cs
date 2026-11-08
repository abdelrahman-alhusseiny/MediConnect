using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using MedicalSystem.Models.ViewModels;
using MedicalSystem.Utilities.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicalSystem.WebUI.Areas.Patient.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize(Roles = SD.Role_Patient)]
    [Area("Patient")]
    [Route("Patient/Notification")]
    public class NotificationController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("GetAllNotifications")]
        public async Task<IActionResult> GetAllNotifications(
            [FromQuery] string searchString = "",
            [FromQuery] string typeStatus = "",
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
            var (notifications, totalItems) =
                await _unitOfWork.Notification.GetFilteredNotificationsAsync(
                    patient.Id, null, searchString, typeStatus, currentSort, currentPage, pageSize);

            var notificationRows = new List<NotificationRowVM>();
            foreach (var n in notifications)
            {
                notificationRows.Add(new NotificationRowVM
                {
                    Notification = n
                });
            }

            var vm = new NotificationListVM
            {
                Notifications = notificationRows,

                SearchString = searchString,

                TypeStatus = typeStatus,
                TypeStatusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = SD.Type_Appointment, Text = "Appointment", Selected = typeStatus == SD.Type_Appointment },
                    new SelectListItem { Value = SD.Type_Payment, Text = "Payment", Selected = typeStatus == SD.Type_Payment },
                    new SelectListItem { Value = SD.Type_System, Text = "System", Selected = typeStatus == SD.Type_System },
                    new SelectListItem { Value = SD.Type_Health, Text = "Health", Selected = typeStatus == SD.Type_Health },
                    new SelectListItem { Value = SD.Type_Account, Text = "Account", Selected = typeStatus == SD.Type_Account },
                    new SelectListItem { Value = SD.Type_Review, Text = "Review", Selected = typeStatus == SD.Type_Review }
                },

                CurrentSort = currentSort,
                NameSort = currentSort == "name" ? "name_desc" : "name",
                CreatedAtSort = currentSort == "created" ? "created_desc" : "created",

                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return View("Areas/Patient/Views/Notification/GetAllNotifications.cshtml", vm);
        }

        [HttpGet("GetNotification/{id:int}")]
        public async Task<IActionResult> GetAppointment(
            [FromRoute] int id, [FromQuery] NotificationVM vm)
        {
            var notification = await _unitOfWork.Notification.GetFirstOrDefaultAsync(n => n.Id == id);
            if (notification == null)
            {
                return NotFound();
            }

            if (notification.Status == SD.Status_Unreaded)
            {
                notification.Status = SD.Status_Readed;
                await _unitOfWork.Notification.UpdateAsync(notification);
                await _unitOfWork.SaveAsync();
            }

            vm.Notification = notification;
            return View("Areas/Patient/Views/Notification/GetNotification.cshtml", vm);
        }

        [HttpPost("DiscardNotification")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscardNotification([FromForm] NotificationVM vm)
        {
            var notification = await _unitOfWork.Notification.GetFirstOrDefaultAsync(
                n => n.Id == vm.Notification.Id);

            if (notification == null)
            {
                return NotFound();
            }

            _unitOfWork.Notification.Remove(notification);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Notification discarded successfully.";
            return RedirectToAction("GetAllNotifications", "Notification", new
            {
                area = "Patient",
                searchString = vm.SearchString,
                typeStatus = vm.TypeStatus,
                currentSort = vm.CurrentSort,
                currentPage = vm.CurrentPage
            });
        }
    }
}
