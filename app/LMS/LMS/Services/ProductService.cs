using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class ProductService
    {
        private readonly AuditService _auditService;

        public ProductService(AuditService auditService)
        {
            _auditService = auditService;
        }

        public List<Product> GetAllProducts(string? search = null, string? category = null, bool activeOnly = true)
        {
            using var db = new AppDbContext();
            var query = db.Products.AsNoTracking().AsQueryable();

            if (activeOnly)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(p => p.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductCode.ToLower().Contains(s) ||
                    p.Name.ToLower().Contains(s) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(s)) ||
                    (p.Brand != null && p.Brand.ToLower().Contains(s)) ||
                    (p.Model != null && p.Model.ToLower().Contains(s)) ||
                    (p.SerialNumber != null && p.SerialNumber.ToLower().Contains(s))
                );
            }

            return query.OrderBy(p => p.Name).ToList();
        }

        public Product? GetProductById(int id)
        {
            using var db = new AppDbContext();
            return db.Products
                .Include(p => p.StockMovements)
                .FirstOrDefault(p => p.Id == id);
        }

        public Product? GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;
            using var db = new AppDbContext();
            string b = barcode.Trim();
            return db.Products.FirstOrDefault(p => p.Barcode == b || p.ProductCode == b || p.SerialNumber == b);
        }

        public List<Product> GetLowStockProducts()
        {
            using var db = new AppDbContext();
            return db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.TrackStock && p.CurrentStock <= p.MinimumStockLevel)
                .OrderBy(p => p.CurrentStock)
                .ToList();
        }

        public string GenerateNextProductCode()
        {
            using var db = new AppDbContext();
            int count = db.Products.Count();
            return $"PRD-{(count + 1):D4}";
        }

        public (bool Success, string Message, Product? Product) SaveProduct(Product product, int currentUserId, string username = "System")
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return (false, "Product Name is required.", null);
            }

            using var db = new AppDbContext();

            if (string.IsNullOrWhiteSpace(product.ProductCode))
            {
                int count = db.Products.Count();
                product.ProductCode = $"PRD-{(count + 1):D4}";
            }

            // Check duplicate code
            bool duplicateCode = db.Products.Any(p => p.ProductCode == product.ProductCode && p.Id != product.Id);
            if (duplicateCode)
            {
                return (false, $"Product code '{product.ProductCode}' already exists.", null);
            }

            if (product.Id == 0)
            {
                // New product
                product.CreatedAt = DateTime.Now;
                db.Products.Add(product);
                db.SaveChanges();

                // If opening stock > 0, record opening stock movement
                if (product.TrackStock && product.CurrentStock > 0)
                {
                    var openingMovement = new StockMovement
                    {
                        ProductId = product.Id,
                        MovementDate = DateTime.Now,
                        MovementType = StockMovementType.OpeningStock,
                        Quantity = product.CurrentStock,
                        UnitPrice = product.PurchasePrice,
                        Reference = "OPENING",
                        Remarks = "Initial opening stock upon product creation",
                        UserId = currentUserId,
                        Username = username,
                        CreatedAt = DateTime.Now
                    };
                    db.StockMovements.Add(openingMovement);
                    db.SaveChanges();
                }

                _auditService.Log(currentUserId, username, "CREATE", "Product", product.Id.ToString(), $"Created product '{product.Name}' ({product.ProductCode}) with opening stock {product.CurrentStock}");
                return (true, "Product created successfully.", product);
            }
            else
            {
                var existing = db.Products.FirstOrDefault(p => p.Id == product.Id);
                if (existing == null)
                {
                    return (false, "Product not found.", null);
                }

                existing.ProductCode = product.ProductCode;
                existing.Barcode = product.Barcode;
                existing.Name = product.Name;
                existing.Category = product.Category;
                existing.Brand = product.Brand;
                existing.Model = product.Model;
                existing.SerialNumber = product.SerialNumber;
                existing.PurchasePrice = product.PurchasePrice;
                existing.CashSalePrice = product.CashSalePrice;
                existing.InstallmentSalePrice = product.InstallmentSalePrice;
                existing.MinimumStockLevel = product.MinimumStockLevel;
                existing.Unit = product.Unit;
                existing.Warranty = product.Warranty;
                existing.TrackStock = product.TrackStock;
                existing.IsActive = product.IsActive;
                existing.Notes = product.Notes;

                db.SaveChanges();

                _auditService.Log(currentUserId, username, "UPDATE", "Product", existing.Id.ToString(), $"Updated product '{existing.Name}' ({existing.ProductCode})");
                return (true, "Product updated successfully.", existing);
            }
        }

        public (bool Success, string Message) DeleteProduct(int id, int currentUserId, string username = "System")
        {
            using var db = new AppDbContext();
            var product = db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return (false, "Product not found.");

            // Check if product has sales
            bool hasSales = db.SaleItems.Any(i => i.ProductId == id);
            if (hasSales)
            {
                // Soft delete
                product.IsActive = false;
                db.SaveChanges();
                _auditService.Log(currentUserId, username, "DEACTIVATE", "Product", id.ToString(), $"Deactivated product '{product.Name}' due to existing sales.");
                return (true, "Product has sale records, so it has been marked inactive instead of permanently deleted.");
            }

            var movements = db.StockMovements.Where(m => m.ProductId == id).ToList();
            if (movements.Any())
            {
                db.StockMovements.RemoveRange(movements);
            }

            db.Products.Remove(product);
            db.SaveChanges();

            _auditService.Log(currentUserId, username, "DELETE", "Product", id.ToString(), $"Deleted product '{product.Name}' ({product.ProductCode})");
            return (true, "Product deleted successfully.");
        }

        public List<string> GetCategories()
        {
            using var db = new AppDbContext();
            var list = db.Products
                .Select(p => p.Category)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => c)
                .ToList();

            if (!list.Contains("Mobile")) list.Add("Mobile");
            if (!list.Contains("Electronics")) list.Add("Electronics");
            if (!list.Contains("Appliances")) list.Add("Appliances");
            if (!list.Contains("Furniture")) list.Add("Furniture");
            if (!list.Contains("Motorcycle")) list.Add("Motorcycle");
            if (!list.Contains("General Merchandise")) list.Add("General Merchandise");

            return list.Distinct().OrderBy(c => c).ToList();
        }
    }
}
