using MedicalSystem.DataAccess.Data;
using MedicalSystem.DataAccess.Repository.IRepository;
using MedicalSystem.Models.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository 
    {
        private readonly ApplicationDbContext _db;
        public AppointmentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Appointment>, int)> GetFilteredAppointmentsAsync(
            int? patientId,
            int? doctorId,
            string searchString,
            int? clinicId,
            string dayOfWeek,
            string currentSort,
            int currentPage,
            int pageSize,
            string[] statusFilter = null)
        {
            var query = _db.Appointments.Include(a => a.Schedule).ThenInclude(s => s.Clinic).ThenInclude(c => c.Doctor).ThenInclude(d => d.Department).Include(a => a.Payment).AsQueryable();

            if (patientId.HasValue)
            {
                query = query.Where(a => a.PatientId == patientId.Value);
            }

            if (doctorId.HasValue)
            {
                query = query.Where(a => a.Schedule.Clinic.DoctorId == doctorId.Value);
            }

            if (statusFilter != null && statusFilter.Any())
            {
                query = query.Where(a => statusFilter.Contains(a.Status));
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(a => a.Schedule.Clinic.Name.Contains(searchString));
            }

            if (clinicId.HasValue)
            {
                query = query.Where(a => a.Schedule.ClinicId == clinicId);
            }

            if (!string.IsNullOrWhiteSpace(dayOfWeek))
            {
                query = query.Where(a => a.Schedule.DayOfWeek == dayOfWeek);
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(a => a.Schedule.Clinic.Name),
                "name_desc" => query.OrderByDescending(a => a.Schedule.Clinic.Name),
                "patient" => query.OrderBy(a => a.PatientName),
                "patient_desc" => query.OrderByDescending(a => a.PatientName),
                "date" => query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime),
                "date_desc" => query.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.AppointmentTime),
                _ => query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime)
            };

            int totalItems = await query.CountAsync();

            var appointments = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (appointments, totalItems);
        }

        public async Task UpdateAsync(Appointment appointment)
        {
            var objFromDb = await _db.Appointments.FirstOrDefaultAsync(
                c => c.Id == appointment.Id);

            if (objFromDb != null)
            {
                objFromDb.PatientName = appointment.PatientName;
                objFromDb.PatientPhoneNumber = appointment.PatientPhoneNumber;
                objFromDb.PatientGender = appointment.PatientGender;
                objFromDb.PatientAge = appointment.PatientAge;
                objFromDb.PatientAddress = appointment.PatientAddress;
                objFromDb.PatientImageUrl = appointment.PatientImageUrl;
                objFromDb.ReasonForVisit = appointment.ReasonForVisit;
                objFromDb.PaymentMethod = appointment.PaymentMethod;
                objFromDb.Status = appointment.Status;
            }
        }
    }
}
