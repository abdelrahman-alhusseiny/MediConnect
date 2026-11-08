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
    public class NotificationRepsotiroy : Repository<Notification>, INotificationRepository
    {
        private readonly ApplicationDbContext _db;
        public NotificationRepsotiroy(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<Notification>, int)> GetFilteredNotificationsAsync(
            int? patientId,
            int? doctorId, 
            string searchString,
            string typeFilter,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.Notifications.AsQueryable();

            if (patientId.HasValue)
            {
                query = query.Where(n => n.PatientId == patientId.Value);
            }

            if (doctorId.HasValue)
            {
                query = query.Where(n => n.DoctorId == doctorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(n => n.Title.Contains(searchString) || n.Message.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(n => n.Type == typeFilter);
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(n => n.Title),
                "name_desc" => query.OrderByDescending(n => n.Title),
                "created" => query.OrderBy(n => n.CreatedAt),
                "created_desc" => query.OrderByDescending(n => n.CreatedAt),
                _ => query.OrderByDescending(n => n.CreatedAt)
            };

            int totalItems = await query.CountAsync();

            var notifications = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalItems);
        }

        public async Task UpdateAsync(Notification notification)
        {
            var objFromDb = await _db.Notifications.FirstOrDefaultAsync(
                n => n.Id == notification.Id);

            if (objFromDb != null)
            {
                objFromDb.Title = notification.Title;
                objFromDb.Message = notification.Message;
                objFromDb.Type = notification.Type;
                objFromDb.Status = notification.Status;
            }
        }
    }
}
