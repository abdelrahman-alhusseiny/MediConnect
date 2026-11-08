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
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasOne(n => n.Patient)
                  .WithMany(p => p.Notifications)
                  .HasForeignKey(n => n.PatientId)
                  .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(n => n.Doctor)
                  .WithMany(d => d.Notifications)
                  .HasForeignKey(n => n.DoctorId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
