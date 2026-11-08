using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class DoctorReviewRowVM
    {
        public DoctorReview DoctorReview { get; set; }
        public string? PatientImageUrl { get; set; }
        public string? PatientName { get; set; }
    }

    public class DoctorReviewListVM
    {
        public IEnumerable<DoctorReviewRowVM> DoctorReviews { get; set; }
        public string? SourceView { get; set; }

        public bool HasPatientReviewed { get; set; }
        public double AverageRating { get; set; } = 0;

        public int DoctorId { get; set; }
        public string? SearchString { get; set; }
        public int? DepartmentId { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
