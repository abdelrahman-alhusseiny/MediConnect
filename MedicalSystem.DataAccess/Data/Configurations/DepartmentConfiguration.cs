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
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.HasData(
            new Department { Id = 1, Name = "Cardiology", Description = "Specializes in heart and vascular system care, including diagnosis and treatment of cardiovascular diseases." },
            new Department { Id = 2, Name = "Dermatology", Description = "Focuses on skin health, treating conditions such as acne, eczema, psoriasis, and skin cancer." },
            new Department { Id = 3, Name = "Neurology", Description = "Deals with disorders of the brain, spinal cord, and nervous system, including epilepsy and stroke." },
            new Department { Id = 4, Name = "Orthopedics", Description = "Provides care for bones, joints, ligaments, and muscles, including fractures and arthritis." },
            new Department { Id = 5, Name = "Pediatrics", Description = "Dedicated to the medical care of infants, children, and adolescents." },
            new Department { Id = 6, Name = "Psychiatry", Description = "Specializes in mental health, including depression, anxiety, and behavioral disorders." },
            new Department { Id = 7, Name = "Gynecology", Description = "Focuses on women’s reproductive health, including pregnancy, fertility, and menstrual disorders." },
            new Department { Id = 8, Name = "Ophthalmology", Description = "Provides eye care, including vision correction, cataract treatment, and management of eye diseases." },
            new Department { Id = 9, Name = "ENT (Otolaryngology)", Description = "Treats conditions of the ear, nose, and throat, including hearing loss and sinus problems." },
            new Department { Id = 10, Name = "General Surgery", Description = "Covers surgical procedures for a wide range of conditions, including abdominal and trauma surgery." }
        );
        }
    }
}
