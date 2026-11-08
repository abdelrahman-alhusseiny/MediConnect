using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.ViewModels
{
    public class DepartmentRowVM
    {
        public Department Department { get; set; }
    }

    public class DepartmentIndexVM
    {
        public IEnumerable<DepartmentRowVM> Departments { get; set; }

        public string? SearchString { get; set; }
        public string? CurrentSort { get; set; }
        public string? NameSort { get; set; }
        public string? CreatedAtSort { get; set; }
        public int? CurrentPage { get; set; }
        public int? TotalPages { get; set; }
    }
}
