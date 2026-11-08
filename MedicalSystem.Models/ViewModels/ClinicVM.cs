using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class ClinicVM
    {
        public Clinic Clinic { get; set; }
        public string? SourceView { get; set; }


        public string? SearchString { get; set; }
        public string? CurrentSort { get; set; }
        public int? CurrentPage { get; set; }
    }
}
