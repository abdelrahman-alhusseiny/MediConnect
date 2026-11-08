using MedicalSystem.Models.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class ClinicRowVM
    {
        public Clinic Clinic { get; set; }
    }

    public class ClinicListVM
    {
        public IEnumerable<ClinicRowVM> Clinics { get; set; }
        public string? SourceView { get; set; }


        public int? DoctorId { get; set; }
        public string? SearchString { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? CreatedAtSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
