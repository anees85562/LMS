using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class AuditService
    {
        public void Log(int? userId, string username, string action, string entityName, string? entityId, string details)
        {
            try
            {
                using var db = new AppDbContext();
                var log = new AuditLog
                {
                    Timestamp = DateTime.Now,
                    UserId = userId,
                    Username = string.IsNullOrWhiteSpace(username) ? "System" : username,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    MachineName = Environment.MachineName
                };

                db.AuditLogs.Add(log);
                db.SaveChanges();
            }
            catch
            {
                // Never allow logging failure to crash the application
            }
        }

        public static void Log(string action, string entityName, string? entityId, string details)
        {
            try
            {
                using var db = new AppDbContext();
                var log = new AuditLog
                {
                    Timestamp = DateTime.Now,
                    UserId = AuthService.CurrentUser?.Id,
                    Username = AuthService.CurrentUser?.Username ?? "System",
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    MachineName = Environment.MachineName
                };

                db.AuditLogs.Add(log);
                db.SaveChanges();
            }
            catch
            {
                // Never allow logging failure to crash the application
            }
        }

        public static List<AuditLog> GetLogs(DateTime? fromDate = null, DateTime? toDate = null, string? username = null, string? action = null, int limit = 500)
        {
            using var db = new AppDbContext();
            var query = db.AuditLogs.AsNoTracking().AsQueryable();

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(l => l.Timestamp >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.Timestamp <= end);
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                query = query.Where(l => l.Username.ToLower().Contains(username.Trim().ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(l => l.Action.ToLower().Contains(action.Trim().ToLower()));
            }

            return query.OrderByDescending(l => l.Timestamp).Take(limit).ToList();
        }
    }
}
