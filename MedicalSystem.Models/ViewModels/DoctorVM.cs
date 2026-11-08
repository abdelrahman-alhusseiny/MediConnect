using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class DoctorVM
    {
        public Doctor Doctor { get; set; }
        public string? SourceView { get; set; }
        public IEnumerable<SelectListItem>? GenderList { get; set; }
        public IEnumerable<SelectListItem>? DepartmentList { get; set; }


        public string? DisplayImageUrl { get; set; }
        public bool HasCustomImage { get; set; } = false;
        public int? Age { get; set; }
        public string? DepartmentName { get; set; }
        public string? DepartmentDescription { get; set; }


        public string? AppUserEmail { get; set; }
        public string? AppUserLockoutStatus { get; set; }


        public double? AverageRating { get; set; }


        public string? SearchString { get; set; }
        public string? LockoutStatus { get; set; }
        public int? DepartmentId { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
