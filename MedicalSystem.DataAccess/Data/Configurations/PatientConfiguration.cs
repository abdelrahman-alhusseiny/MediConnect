using MedicalSystem.Models.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Data.Configurations
{
    public class PatientConfiguration : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.HasOne(c => c.AppUser)
                    .WithOne(u => u.Patient)
                    .HasForeignKey<Patient>(c => c.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
