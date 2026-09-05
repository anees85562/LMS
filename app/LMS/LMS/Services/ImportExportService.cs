using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class ImportExportService
    {
        public static void ExportToCsv(DataTable dt, string filePath)
        {
            var sb = new StringBuilder();

            // Headers
            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().Select(column => $"\"{column.ColumnName.Replace("\"", "\"\"")}\"");
            sb.AppendLine(string.Join(",", columnNames));

            // Rows
            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                {
                    string str = field?.ToString() ?? "";
                    return $"\"{str.Replace("\"", "\"\"")}\"";
                });
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportToHtmlExcel(DataTable dt, string title, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Segoe UI, Arial, sans-serif; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("th { background-color: #2563EB; color: white; padding: 8px; border: 1px solid #CBD5E1; text-align: left; }");
            sb.AppendLine("td { padding: 6px 8px; border: 1px solid #E2E8F0; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #F8FAFC; }");
            sb.AppendLine(".title { font-size: 18px; font-weight: bold; margin-bottom: 4px; color: #1E293B; }");
            sb.AppendLine(".subtitle { font-size: 12px; color: #64748B; margin-bottom: 12px; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine($"<div class='title'>{title}</div>");
            sb.AppendLine($"<div class='subtitle'>Exported on {DateTime.Now:dd/MM/yyyy HH:mm}</div>");

            sb.AppendLine("<table>");
            sb.AppendLine("<thead><tr>");
            foreach (DataColumn col in dt.Columns)
            {
                sb.AppendLine($"<th>{col.ColumnName}</th>");
            }
            sb.AppendLine("</tr></thead><tbody>");

            foreach (DataRow row in dt.Rows)
            {
                sb.AppendLine("<tr>");
                foreach (var item in row.ItemArray)
                {
                    string str = item?.ToString() ?? "";
                    sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(str)}</td>");
                }
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static string GenerateTenantCsvTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TenantCode,FullName,FatherOrHusbandName,CnicOrId,ContactNumber,AlternateContact,PermanentAddress,Notes");
            sb.AppendLine("TEN-001,Muhammad Ali,Ahmad Ali,35201-1234567-1,03001234567,03217654321,House #12 Street 3 Lahore,Initial tenant record");
            sb.AppendLine("TEN-002,Usman Khan,Tariq Khan,35202-9876543-2,03339876543,,Flat 4B Gulberg Lahore,Commercial office");
            return sb.ToString();
        }

        public static string GeneratePropertyCsvTemplate()
        {
            var sb = new StringBuilder();
            sb.AppendLine("PropertyCode,PropertyName,PropertyType,Address,City,UnitNumber,UnitType,Floor,BaseRent");
            sb.AppendLine("PROP-001,Madina Commercial Plaza,Commercial,Main Market Gulberg,Lahore,Shop 1,Shop,Ground Floor,35000");
            sb.AppendLine("PROP-001,Madina Commercial Plaza,Commercial,Main Market Gulberg,Lahore,Shop 2,Shop,Ground Floor,30000");
            sb.AppendLine("PROP-001,Madina Commercial Plaza,Commercial,Main Market Gulberg,Lahore,Flat 101,Flat,1st Floor,25000");
            sb.AppendLine("PROP-002,Green Heights,Residential,Model Town Block C,Lahore,Portion A,Portion,Ground,40000");
            return sb.ToString();
        }

        public static (bool Success, string Message, int ImportedCount, int SkippedCount, List<string> Errors) ImportTenantsFromCsv(string csvFilePath)
        {
            var errors = new List<string>();
            int imported = 0;
            int skipped = 0;

            if (!File.Exists(csvFilePath))
            {
                return (false, "CSV file not found.", 0, 0, new List<string> { "File does not exist." });
            }

            // Create pre-import safety backup
            BackupService.CreateBackup(null, BackupType.PreImport, "Pre-tenant import safety backup");

            using var db = new AppDbContext();
            using var tx = db.Database.BeginTransaction();
            try
            {
                using var reader = new StreamReader(csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null
                });

                var records = csv.GetRecords<dynamic>().ToList();
                int rowNum = 1;

                foreach (var row in records)
                {
                    rowNum++;
                    var dict = (IDictionary<string, object>)row;

                    string name = GetDictVal(dict, "FullName") ?? GetDictVal(dict, "Name") ?? "";
                    string contact = GetDictVal(dict, "ContactNumber") ?? GetDictVal(dict, "Phone") ?? GetDictVal(dict, "Contact") ?? "";
                    string code = GetDictVal(dict, "TenantCode") ?? GetDictVal(dict, "Code") ?? "";

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add($"Row {rowNum}: Skipped - Full Name is empty.");
                        skipped++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(contact))
                    {
                        errors.Add($"Row {rowNum}: Skipped - Contact number is empty.");
                        skipped++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(code) || db.Tenants.Any(t => t.TenantCode == code))
                    {
                        int tCount = db.Tenants.Count() + 1;
                        code = $"TEN-{tCount:D3}";
                    }

                    var tenant = new Tenant
                    {
                        TenantCode = code,
                        FullName = name.Trim(),
                        FatherOrHusbandName = GetDictVal(dict, "FatherOrHusbandName") ?? GetDictVal(dict, "FatherName"),
                        CnicOrId = GetDictVal(dict, "CnicOrId") ?? GetDictVal(dict, "CNIC"),
                        ContactNumber = contact.Trim(),
                        AlternateContact = GetDictVal(dict, "AlternateContact"),
                        PermanentAddress = GetDictVal(dict, "PermanentAddress") ?? GetDictVal(dict, "Address"),
                        Notes = GetDictVal(dict, "Notes"),
                        Status = TenantStatus.Active,
                        CreatedAt = DateTime.Now
                    };

                    db.Tenants.Add(tenant);
                    db.SaveChanges();
                    imported++;
                }

                tx.Commit();
                AuditService.Log("Import Tenants", "Tenant", imported.ToString(), $"Successfully imported {imported} tenants from CSV. Skipped: {skipped}.");
                return (true, $"Import completed: {imported} tenants imported, {skipped} skipped.", imported, skipped, errors);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, $"Import failed: {ex.Message}", 0, 0, new List<string> { ex.Message });
            }
        }

        public static (bool Success, string Message, int ImportedProps, int ImportedUnits, List<string> Errors) ImportPropertiesFromCsv(string csvFilePath)
        {
            var errors = new List<string>();
            int importedProps = 0;
            int importedUnits = 0;

            if (!File.Exists(csvFilePath))
            {
                return (false, "CSV file not found.", 0, 0, new List<string> { "File does not exist." });
            }

            // Create pre-import safety backup
            BackupService.CreateBackup(null, BackupType.PreImport, "Pre-property import safety backup");

            using var db = new AppDbContext();
            using var tx = db.Database.BeginTransaction();
            try
            {
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

                using var reader = new StreamReader(csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    HeaderValidated = null
                });

                var records = csv.GetRecords<dynamic>().ToList();
                int rowNum = 1;

                var propCache = new Dictionary<string, Property>();

                foreach (var row in records)
                {
                    rowNum++;
                    var dict = (IDictionary<string, object>)row;

                    string propName = GetDictVal(dict, "PropertyName") ?? GetDictVal(dict, "Property") ?? "";
                    string propCode = GetDictVal(dict, "PropertyCode") ?? "";
                    string unitNumber = GetDictVal(dict, "UnitNumber") ?? GetDictVal(dict, "Unit") ?? "";

                    if (string.IsNullOrWhiteSpace(propName))
                    {
                        errors.Add($"Row {rowNum}: Skipped - Property Name is empty.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(unitNumber))
                    {
                        unitNumber = "Unit 1";
                    }

                    // Find or create property
                    Property? prop = null;
                    if (!string.IsNullOrWhiteSpace(propCode) && propCache.ContainsKey(propCode))
                    {
                        prop = propCache[propCode];
                    }
                    else if (propCache.ContainsKey(propName))
                    {
                        prop = propCache[propName];
                    }
                    else
                    {
                        prop = db.Properties.FirstOrDefault(p => p.Name.ToLower() == propName.Trim().ToLower());
                        if (prop == null)
                        {
                            int pCount = db.Properties.Count() + 1;
                            string code = string.IsNullOrWhiteSpace(propCode) ? $"PROP-{pCount:D3}" : propCode;
                            prop = new Property
                            {
                                LandlordId = landlord.Id,
                                PropertyCode = code,
                                Name = propName.Trim(),
                                PropertyType = GetDictVal(dict, "PropertyType") ?? "Residential",
                                Address = GetDictVal(dict, "Address"),
                                City = GetDictVal(dict, "City"),
                                Status = PropertyStatus.Active,
                                CreatedAt = DateTime.Now
                            };
                            db.Properties.Add(prop);
                            db.SaveChanges();
                            importedProps++;
                        }

                        propCache[propName] = prop;
                        if (!string.IsNullOrWhiteSpace(propCode)) propCache[propCode] = prop;
                    }

                    // Add unit if not already existing
                    string rentStr = GetDictVal(dict, "BaseRent") ?? GetDictVal(dict, "Rent") ?? "0";
                    decimal.TryParse(rentStr, out decimal baseRent);

                    bool unitExists = db.PropertyUnits.Any(u => u.PropertyId == prop.Id && u.UnitNumber.ToLower() == unitNumber.Trim().ToLower());
                    if (!unitExists)
                    {
                        var unit = new PropertyUnit
                        {
                            PropertyId = prop.Id,
                            UnitNumber = unitNumber.Trim(),
                            UnitType = GetDictVal(dict, "UnitType") ?? "Portion",
                            Floor = GetDictVal(dict, "Floor"),
                            BaseRent = baseRent,
                            Status = UnitStatus.Vacant,
                            CreatedAt = DateTime.Now
                        };
                        db.PropertyUnits.Add(unit);
                        db.SaveChanges();
                        importedUnits++;
                    }
                }

                tx.Commit();
                AuditService.Log("Import Properties", "Property", importedProps.ToString(), $"Imported {importedProps} properties and {importedUnits} units from CSV.");
                return (true, $"Import completed: {importedProps} properties and {importedUnits} units imported.", importedProps, importedUnits, errors);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, $"Import failed: {ex.Message}", 0, 0, new List<string> { ex.Message });
            }
        }

        private static string? GetDictVal(IDictionary<string, object> dict, string key)
        {
            var match = dict.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
            if (match != null && dict[match] != null)
            {
                return dict[match].ToString()?.Trim();
            }
            return null;
        }
    }
}
