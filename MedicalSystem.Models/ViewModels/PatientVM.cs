using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class PatientVM
    {
        public Patient Patient { get; set; }
        public string? SourceView { get; set; }
        public IEnumerable<SelectListItem>? GenderList { get; set; }


        public string? DisplayImageUrl { get; set; }
        public bool HasCustomImage { get; set; } = false;


        public string? AppUserEmail { get; set; }
        public string? AppUserLockoutStatus { get; set; }


        public string? SearchString { get; set; }
        public string? LockoutStatus { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
