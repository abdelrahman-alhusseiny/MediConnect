using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class PatientRowVM
    {
        public Patient Patient { get; set; }
        public string? DisplayImageUrl { get; set; }
        public string? AppUserEmail { get; set; }
        public string? AppUserLockoutStatus { get; set; }
    }

    public class PatientListVM
    {
        public IEnumerable<PatientRowVM> Patients { get; set; }
        public string? SourceView { get; set; }


        public string? SearchString { get; set; }
        public string? LockoutStatus { get; set; }
        public IEnumerable<SelectListItem>? LockoutStatusList { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? CreatedAtSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
