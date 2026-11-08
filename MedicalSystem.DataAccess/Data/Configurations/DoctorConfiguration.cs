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
    public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.HasOne(p => p.Department)
                  .WithMany(c => c.Doctors)
                  .HasForeignKey(p => p.DepartmentId)
                  .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.AppUser)
                    .WithOne(u => u.Doctor)
                    .HasForeignKey<Doctor>(c => c.AppUserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
