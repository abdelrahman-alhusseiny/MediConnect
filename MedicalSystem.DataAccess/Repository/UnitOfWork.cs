using MedicalSystem.DataAccess.Data;
using MedicalSystem.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        public IDepartmentRepository Department { get; private set; }
        public IPatientRepository Patient { get; private set; }
        public IDoctorRepository Doctor { get; private set; }
        public IClinicRepository Clinic { get; private set; }
        public IScheduleRepository Schedule { get; private set; }
        public IAppointmentRepository Appointment { get; private set; }
        public INotificationRepository Notification { get; private set; }
        public IPaymentRepository Payment { get; private set; }
        public IDoctorReviewRepository DoctorReview { get; private set; }

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Department = new DepartmentRepository(_db);
            Patient = new PatientRepository(_db);
            Doctor = new DoctorRepository(_db);
            Clinic = new ClinicRepository(_db);
            Schedule = new ScheduleRepository(_db);
            Appointment = new AppointmentRepository(_db);
            Notification = new NotificationRepsotiroy(_db);
            Payment = new PaymentRepository(_db);
            DoctorReview = new DoctorReviewRepository(_db);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
