using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        IDepartmentRepository Department { get; }
        IPatientRepository Patient { get; }
        IDoctorRepository Doctor { get; }
        IClinicRepository Clinic { get; }
        IScheduleRepository Schedule { get; }
        IAppointmentRepository Appointment { get; }
        INotificationRepository Notification { get; }
        IPaymentRepository Payment { get; }
        IDoctorReviewRepository DoctorReview { get; }
        Task SaveAsync();
    }
}
