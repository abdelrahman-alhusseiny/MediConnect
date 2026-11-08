using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Models.DTOs.Password
{
    public class ResetPasswordDto
    {
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
