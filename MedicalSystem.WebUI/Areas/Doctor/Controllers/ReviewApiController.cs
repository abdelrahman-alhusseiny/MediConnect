using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.DTOs.DoctorReview;
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
    public class ReviewApiController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewApiController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("GetAllReviews")]
        public async Task<ActionResult<IEnumerable<GetAllReviews>>> GetAllReviews()
        {
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null)
            {
                return NotFound("User not found.");
            }

            var doctor = await _unitOfWork.Doctor.GetFirstOrDefaultAsync(
                d => d.AppUserId == appUser.Id);
           
            if (doctor == null)
            {
                return NotFound("Doctor not found.");
            }

            var doctorReviews = await _unitOfWork.DoctorReview.GetFilteredDoctorReviewsAsync(doctor.Id);
            if (doctorReviews == null || !doctorReviews.Any())
            {
                return Ok(new
                {
                    AverageRating = 0,
                    Reviews = new List<GetAllReviews>()
                });
            }

            var reviewDtos = doctorReviews.Select(r => new GetAllReviews
            {
                AverageRating = 0, 
                PatientImageUrl = !string.IsNullOrEmpty(r.Patient.ImageUrl)
                    ? r.Patient.ImageUrl
                    : (r.Patient.Gender?.ToLower() == "male"
                        ? "/images/patients/default-patient-male.png"
                        : "/images/patients/default-patient-female.png"),
                PatientName = r.Patient.FullName,
                Rating = r.Rating,
                Comment = r.Comment ?? string.Empty,
                CreatedAt = r.CreatedAt
            }).ToList();

            double avgRating = await _unitOfWork.DoctorReview.GetAverageRatingAsync(doctor.Id);
            foreach (var dto in reviewDtos)
            {
                dto.AverageRating = avgRating;
            }

            return Ok(new
            {
                AverageRating = avgRating,
                Reviews = reviewDtos
            });
        }
    }
}
