using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class PropertyService
    {
        // ----------------- Landlord Management -----------------
        public static Landlord GetOrCreateDefaultLandlord()
        {
            using var db = new AppDbContext();
            var landlord = db.Landlords.FirstOrDefault(l => l.IsActive);
            if (landlord == null)
            {
                landlord = new Landlord
                {
                    Name = SettingService.Get("General.OwnerName", "Property Owner"),
                    Phone = SettingService.Get("General.Phone", "+92 300 0000000"),
                    Address = SettingService.Get("General.Address", "Main City"),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                db.Landlords.Add(landlord);
                db.SaveChanges();
            }
            return landlord;
        }

        public static List<Landlord> GetAllLandlords()
        {
            using var db = new AppDbContext();
            return db.Landlords.AsNoTracking().OrderBy(l => l.Name).ToList();
        }

        public static (bool Success, string Message, Landlord? Landlord) SaveLandlord(Landlord model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return (false, "Landlord name is required.", null);
            }

            using var db = new AppDbContext();
            if (model.Id == 0)
            {
                model.CreatedAt = DateTime.Now;
                db.Landlords.Add(model);
                db.SaveChanges();
                AuditService.Log("Create Landlord", "Landlord", model.Id.ToString(), $"Created landlord '{model.Name}'");
                return (true, "Landlord created successfully.", model);
            }
            else
            {
                var existing = db.Landlords.Find(model.Id);
                if (existing == null) return (false, "Landlord not found.", null);

                existing.Name = model.Name.Trim();
                existing.FatherOrHusbandName = model.FatherOrHusbandName?.Trim();
                existing.CnicOrId = model.CnicOrId?.Trim();
                existing.Phone = model.Phone?.Trim();
                existing.Email = model.Email?.Trim();
                existing.Address = model.Address?.Trim();
                existing.Notes = model.Notes?.Trim();
                existing.IsActive = model.IsActive;

                db.SaveChanges();
                AuditService.Log("Update Landlord", "Landlord", existing.Id.ToString(), $"Updated landlord '{existing.Name}'");
                return (true, "Landlord updated successfully.", existing);
            }
        }

        // ----------------- Property Management -----------------
        public static string GenerateNextPropertyCode()
        {
            using var db = new AppDbContext();
            int count = db.Properties.Count() + 1;
            string code = $"PROP-{count:D3}";
            while (db.Properties.Any(p => p.PropertyCode == code))
            {
                count++;
                code = $"PROP-{count:D3}";
            }
            return code;
        }

        public static List<Property> GetAllProperties(bool includeInactive = false)
        {
            using var db = new AppDbContext();
            var query = db.Properties
                          .Include(p => p.Landlord)
                          .Include(p => p.Units)
                          .AsNoTracking()
                          .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(p => p.Status == PropertyStatus.Active);
            }

            return query.OrderBy(p => p.Name).ToList();
        }

        public static Property? GetPropertyById(int propertyId)
        {
            using var db = new AppDbContext();
            return db.Properties
                     .Include(p => p.Landlord)
                     .Include(p => p.Units)
                     .FirstOrDefault(p => p.Id == propertyId);
        }

        public static (bool Success, string Message, Property? Property) SaveProperty(Property model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                return (false, "Property name is required.", null);
            }

            using var db = new AppDbContext();

            if (model.LandlordId == 0)
            {
                var landlord = GetOrCreateDefaultLandlord();
                model.LandlordId = landlord.Id;
            }

            if (string.IsNullOrWhiteSpace(model.PropertyCode))
            {
                model.PropertyCode = GenerateNextPropertyCode();
            }

            if (model.Id == 0)
            {
                if (db.Properties.Any(p => p.PropertyCode == model.PropertyCode))
                {
                    model.PropertyCode = GenerateNextPropertyCode();
                }

                model.CreatedAt = DateTime.Now;
                db.Properties.Add(model);
                db.SaveChanges();
                AuditService.Log("Create Property", "Property", model.Id.ToString(), $"Created property '{model.Name}' ({model.PropertyCode})");
                return (true, "Property created successfully.", model);
            }
            else
            {
                var existing = db.Properties.Find(model.Id);
                if (existing == null) return (false, "Property not found.", null);

                if (db.Properties.Any(p => p.PropertyCode == model.PropertyCode && p.Id != model.Id))
                {
                    return (false, $"Property Code '{model.PropertyCode}' is already used by another property.", null);
                }

                existing.LandlordId = model.LandlordId;
                existing.PropertyCode = model.PropertyCode.Trim();
                existing.Name = model.Name.Trim();
                existing.PropertyType = model.PropertyType?.Trim() ?? "Residential";
                existing.Address = model.Address?.Trim();
                existing.City = model.City?.Trim();
                existing.Status = model.Status;
                existing.Notes = model.Notes?.Trim();

                db.SaveChanges();
                AuditService.Log("Update Property", "Property", existing.Id.ToString(), $"Updated property '{existing.Name}' ({existing.PropertyCode})");
                return (true, "Property updated successfully.", existing);
            }
        }

        public static (bool Success, string Message) DeleteOrArchiveProperty(int propertyId)
        {
            using var db = new AppDbContext();
            var property = db.Properties.Include(p => p.Units).FirstOrDefault(p => p.Id == propertyId);
            if (property == null) return (false, "Property not found.");

            var unitIds = property.Units.Select(u => u.Id).ToList();

            bool hasTransactions = db.Transactions.Any(t => t.PropertyUnitId != null && unitIds.Contains(t.PropertyUnitId.Value));
            bool hasAgreements = db.RentAgreements.Any(a => unitIds.Contains(a.PropertyUnitId));

            if (hasTransactions || hasAgreements)
            {
                // Cannot delete physical record due to financial records
                property.Status = PropertyStatus.Archived;
                db.SaveChanges();
                AuditService.Log("Archive Property", "Property", property.Id.ToString(), $"Archived property '{property.Name}' because historical financial/lease records exist.");
                return (true, "Property has associated lease/financial records and was safely ARCHIVED instead of deleted.");
            }

            // Safe to delete completely
            db.PropertyUnits.RemoveRange(property.Units);
            db.Properties.Remove(property);
            db.SaveChanges();

            AuditService.Log("Delete Property", "Property", propertyId.ToString(), $"Deleted empty property '{property.Name}'.");
            return (true, "Property deleted successfully.");
        }

        // ----------------- Unit Management -----------------
        public static List<PropertyUnit> GetUnitsByPropertyId(int propertyId)
        {
            using var db = new AppDbContext();
            return db.PropertyUnits
                     .Include(u => u.Property)
                     .Include(u => u.RentAgreements)
                     .ThenInclude(a => a.Tenant)
                     .Where(u => u.PropertyId == propertyId)
                     .OrderBy(u => u.UnitNumber)
                     .ToList();
        }

        public static List<PropertyUnit> GetAllUnits()
        {
            using var db = new AppDbContext();
            return db.PropertyUnits
                     .Include(u => u.Property)
                     .Include(u => u.RentAgreements)
                     .ThenInclude(a => a.Tenant)
                     .OrderBy(u => u.Property!.Name)
                     .ThenBy(u => u.UnitNumber)
                     .ToList();
        }

        public static (bool Success, string Message, PropertyUnit? Unit) SaveUnit(PropertyUnit model)
        {
            if (string.IsNullOrWhiteSpace(model.UnitNumber))
            {
                return (false, "Unit number/name is required.", null);
            }

            if (model.PropertyId <= 0)
            {
                return (false, "Valid property must be selected.", null);
            }

            using var db = new AppDbContext();
            string unitNum = model.UnitNumber.Trim();

            if (model.Id == 0)
            {
                if (db.PropertyUnits.Any(u => u.PropertyId == model.PropertyId && u.UnitNumber.ToLower() == unitNum.ToLower()))
                {
                    return (false, $"Unit '{unitNum}' already exists for this property.", null);
                }

                model.UnitNumber = unitNum;
                model.CreatedAt = DateTime.Now;
                db.PropertyUnits.Add(model);
                db.SaveChanges();

                AuditService.Log("Create Unit", "PropertyUnit", model.Id.ToString(), $"Created unit '{model.UnitNumber}' in property ID {model.PropertyId}");
                return (true, "Unit created successfully.", model);
            }
            else
            {
                var existing = db.PropertyUnits.Find(model.Id);
                if (existing == null) return (false, "Unit not found.", null);

                if (db.PropertyUnits.Any(u => u.PropertyId == model.PropertyId && u.UnitNumber.ToLower() == unitNum.ToLower() && u.Id != model.Id))
                {
                    return (false, $"Unit '{unitNum}' already exists for this property.", null);
                }

                existing.UnitNumber = unitNum;
                existing.UnitType = model.UnitType?.Trim() ?? "Portion";
                existing.Floor = model.Floor?.Trim();
                existing.BaseRent = model.BaseRent;
                existing.Status = model.Status;
                existing.Notes = model.Notes?.Trim();

                db.SaveChanges();
                AuditService.Log("Update Unit", "PropertyUnit", existing.Id.ToString(), $"Updated unit '{existing.UnitNumber}'");
                return (true, "Unit updated successfully.", existing);
            }
        }

        public static (bool Success, string Message) DeleteUnit(int unitId)
        {
            using var db = new AppDbContext();
            var unit = db.PropertyUnits.Find(unitId);
            if (unit == null) return (false, "Unit not found.");

            bool hasTransactions = db.Transactions.Any(t => t.PropertyUnitId == unitId);
            bool hasAgreements = db.RentAgreements.Any(a => a.PropertyUnitId == unitId);

            if (hasTransactions || hasAgreements)
            {
                return (false, "Cannot delete this unit because rent agreements or financial transactions are linked to it. You can set its status to Under Maintenance instead.");
            }

            db.PropertyUnits.Remove(unit);
            db.SaveChanges();
            AuditService.Log("Delete Unit", "PropertyUnit", unitId.ToString(), $"Deleted unit '{unit.UnitNumber}'");
            return (true, "Unit deleted successfully.");
        }

        // ----------------- Summary & Metrics -----------------
        public static (int TotalProperties, int ActiveProperties, int TotalUnits, int OccupiedUnits, int VacantUnits, double OccupancyRate) GetOccupancyMetrics()
        {
            using var db = new AppDbContext();
            int totalProperties = db.Properties.Count();
            int activeProperties = db.Properties.Count(p => p.Status == PropertyStatus.Active);
            int totalUnits = db.PropertyUnits.Count();
            int occupiedUnits = db.PropertyUnits.Count(u => u.Status == UnitStatus.Occupied);
            int vacantUnits = db.PropertyUnits.Count(u => u.Status == UnitStatus.Vacant);

            double occupancyRate = totalUnits > 0 ? ((double)occupiedUnits / totalUnits) * 100.0 : 0.0;

            return (totalProperties, activeProperties, totalUnits, occupiedUnits, vacantUnits, occupancyRate);
        }
    }
}
