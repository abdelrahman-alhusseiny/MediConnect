using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class DoctorReviewVM
    {
        public DoctorReview DoctorReview { get; set; }
        public string? SourceView { get; set; }

        public IEnumerable<SelectListItem>? RatingList { get; set; }

        public string? PatientImageUrl { get; set; }
        public string? PatientName { get; set; }

        public int DoctorId { get; set; }
        public string? SearchString { get; set; }
        public int? DepartmentId { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
