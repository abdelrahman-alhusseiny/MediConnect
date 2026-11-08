using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.Models
{
    public class ApplicationUser : IdentityUser
    {
        [ValidateNever]
        public Patient Patient { get; set; }

        [ValidateNever]
        public Doctor Doctor { get; set; }
    }
}
