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
    [Route("Doctor/Review")]
    public class ReviewController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        [HttpGet("GetAllReviews")]
        public async Task<IActionResult> GetAllReviews()
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

           var doctorReviews = await _unitOfWork.DoctorReview.GetFilteredDoctorReviewsAsync(doctor.Id);

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

            var vm = new DoctorReviewListVM
            {
                DoctorReviews = doctorReviewRows,
                AverageRating = await _unitOfWork.DoctorReview.GetAverageRatingAsync(doctor.Id)
            };

            return View("Areas/Doctor/Views/Review/GetAllReviews.cshtml", vm);
        }
    }
}
