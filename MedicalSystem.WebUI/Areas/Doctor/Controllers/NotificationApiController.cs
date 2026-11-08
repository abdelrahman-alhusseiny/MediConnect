using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.Notification;
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
    public class NotificationApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationApiController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
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
                return Unauthorized(new
                {
                    Success = false,
                    Message = "User not found."
                });
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
            if (doctor == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Doctor not found."
                });
            }

            int pageSize = 6;
            var (notifications, totalItems) =
                await _unitOfWork.Notification.GetFilteredNotificationsAsync(
                    null, doctor.Id, searchString, typeStatus, currentSort, currentPage, pageSize);

            var notificationDtos = notifications.Select(n => new GetAllNotificationsDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                Status = n.Status
            }).ToList();

            var response = new
            {
                Success = true,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Notifications = notificationDtos
            };

            return Ok(response);
        }

        [HttpGet("GetNotification/{id:int}")]
        public async Task<IActionResult> GetNotification([FromRoute] int id)
        {
            var notification = await _unitOfWork.Notification.GetFirstOrDefaultAsync(n => n.Id == id);
            if (notification == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Notification not found."
                });
            }

            if (notification.Status == SD.Status_Unreaded)
            {
                notification.Status = SD.Status_Readed;
                await _unitOfWork.Notification.UpdateAsync(notification);
                await _unitOfWork.SaveAsync();
            }

            var dto = new GetNotificationDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt,
                Status = notification.Status
            };

            return Ok(new
            {
                Success = true,
                Notification = dto
            });
        }

        [HttpDelete("DiscardNotification/{id:int}")]
        public async Task<IActionResult> DiscardNotification([FromRoute] int id)
        {
            var notification = await _unitOfWork.Notification.GetFirstOrDefaultAsync(
                n => n.Id == id);

            if (notification == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Notification not found."
                });
            }

            _unitOfWork.Notification.Remove(notification);
            await _unitOfWork.SaveAsync();

            return Ok(new
            {
                Success = true,
                Message = "Notification discarded successfully.",
                NotificationId = id
            });
        }
    }
}
