using MedicalSystem.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.DataAccess.Repository.IRepository
{
    public interface INotificationRepository : IRepository<Notification>
    {
        Task<(IEnumerable<Notification>, int)> GetFilteredNotificationsAsync(
            int? patientId,
            int? doctorId,
            string searchString,
            string typeFilter,
            string currentSort,
            int currentPage,
            int pageSize);

        Task UpdateAsync(Notification notification);
    }
}
