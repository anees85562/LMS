using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class InventoryService
    {
        private readonly AuditService _auditService;

        public InventoryService(AuditService auditService)
        {
            _auditService = auditService;
        }

        public (bool Success, string Message) RecordPurchase(
            int productId,
            int quantity,
            decimal unitCost,
            string? reference,
            string? remarks,
            int userId,
            string username)
        {
            if (quantity <= 0) return (false, "Purchase quantity must be greater than 0.");

            using var db = new AppDbContext();
            var product = db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return (false, "Product not found.");

            if (product.TrackStock)
            {
                product.CurrentStock += quantity;
            }

            if (unitCost > 0)
            {
                product.PurchasePrice = unitCost;
            }

            var movement = new StockMovement
            {
                ProductId = productId,
                MovementDate = DateTime.Now,
                MovementType = StockMovementType.Purchase,
                Quantity = quantity,
                UnitPrice = unitCost,
                Reference = reference ?? $"PUR-{DateTime.Now:yyyyMMddHHmm}",
                Remarks = remarks ?? $"Purchase of {quantity} {product.Unit}",
                UserId = userId,
                Username = username,
                CreatedAt = DateTime.Now
            };

            db.StockMovements.Add(movement);
            db.SaveChanges();

            _auditService.Log(userId, username, "PURCHASE", "Inventory", product.Id.ToString(), $"Purchased {quantity} {product.Unit} of '{product.Name}' at {unitCost:N2} each. New stock: {product.CurrentStock}");
            return (true, $"Stock updated successfully. Current stock: {product.CurrentStock}");
        }

        public (bool Success, string Message) RecordSaleDeduction(
            int productId,
            int quantity,
            string invoiceNumber,
            int userId,
            string username)
        {
            if (quantity <= 0) return (false, "Quantity must be greater than 0.");

            using var db = new AppDbContext();
            var product = db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return (false, "Product not found.");

            if (product.TrackStock)
            {
                product.CurrentStock -= quantity;
            }

            var movement = new StockMovement
            {
                ProductId = productId,
                MovementDate = DateTime.Now,
                MovementType = StockMovementType.Sale,
                Quantity = -quantity,
                UnitPrice = product.CashSalePrice,
                Reference = invoiceNumber,
                Remarks = $"Sold via Invoice {invoiceNumber}",
                UserId = userId,
                Username = username,
                CreatedAt = DateTime.Now
            };

            db.StockMovements.Add(movement);
            db.SaveChanges();

            _auditService.Log(userId, username, "SALE_DEDUCTION", "Inventory", product.Id.ToString(), $"Deducted {quantity} {product.Unit} of '{product.Name}' for Invoice {invoiceNumber}. Remaining stock: {product.CurrentStock}");
            return (true, $"Stock deducted. Current stock: {product.CurrentStock}");
        }

        public (bool Success, string Message) RecordReturn(
            int productId,
            int quantity,
            string invoiceNumber,
            string? remarks,
            int userId,
            string username)
        {
            if (quantity <= 0) return (false, "Quantity must be greater than 0.");

            using var db = new AppDbContext();
            var product = db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return (false, "Product not found.");

            if (product.TrackStock)
            {
                product.CurrentStock += quantity;
            }

            var movement = new StockMovement
            {
                ProductId = productId,
                MovementDate = DateTime.Now,
                MovementType = StockMovementType.Return,
                Quantity = quantity,
                UnitPrice = product.CashSalePrice,
                Reference = invoiceNumber,
                Remarks = remarks ?? $"Customer return from Invoice {invoiceNumber}",
                UserId = userId,
                Username = username,
                CreatedAt = DateTime.Now
            };

            db.StockMovements.Add(movement);
            db.SaveChanges();

            _auditService.Log(userId, username, "SALE_RETURN", "Inventory", product.Id.ToString(), $"Returned {quantity} {product.Unit} of '{product.Name}' from Invoice {invoiceNumber}. Current stock: {product.CurrentStock}");
            return (true, $"Stock restocked. Current stock: {product.CurrentStock}");
        }

        public (bool Success, string Message) RecordStockAdjustment(
            int productId,
            int newQuantity,
            string reason,
            int userId,
            string username)
        {
            if (newQuantity < 0) return (false, "Quantity cannot be negative.");

            using var db = new AppDbContext();
            var product = db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return (false, "Product not found.");

            int delta = newQuantity - product.CurrentStock;
            int oldStock = product.CurrentStock;
            product.CurrentStock = newQuantity;

            var movement = new StockMovement
            {
                ProductId = productId,
                MovementDate = DateTime.Now,
                MovementType = StockMovementType.StockAdjustment,
                Quantity = delta,
                UnitPrice = product.PurchasePrice,
                Reference = $"ADJ-{DateTime.Now:yyyyMMdd}",
                Remarks = $"Stock adjustment from {oldStock} to {newQuantity}. Reason: {reason}",
                UserId = userId,
                Username = username,
                CreatedAt = DateTime.Now
            };

            db.StockMovements.Add(movement);
            db.SaveChanges();

            _auditService.Log(userId, username, "STOCK_ADJUSTMENT", "Inventory", product.Id.ToString(), $"Adjusted stock of '{product.Name}' from {oldStock} to {newQuantity}. Reason: {reason}");
            return (true, $"Stock adjusted successfully. Current stock: {product.CurrentStock}");
        }

        public (bool Success, string Message) RecordDamagedStock(
            int productId,
            int damagedQuantity,
            string reason,
            int userId,
            string username)
        {
            if (damagedQuantity <= 0) return (false, "Damaged quantity must be greater than 0.");

            using var db = new AppDbContext();
            var product = db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return (false, "Product not found.");

            if (product.TrackStock)
            {
                product.CurrentStock -= damagedQuantity;
            }

            var movement = new StockMovement
            {
                ProductId = productId,
                MovementDate = DateTime.Now,
                MovementType = StockMovementType.DamagedStock,
                Quantity = -damagedQuantity,
                UnitPrice = product.PurchasePrice,
                Reference = $"DMG-{DateTime.Now:yyyyMMdd}",
                Remarks = $"Damaged / written-off stock: {damagedQuantity} {product.Unit}. Reason: {reason}",
                UserId = userId,
                Username = username,
                CreatedAt = DateTime.Now
            };

            db.StockMovements.Add(movement);
            db.SaveChanges();

            _auditService.Log(userId, username, "DAMAGED_STOCK", "Inventory", product.Id.ToString(), $"Recorded {damagedQuantity} damaged {product.Unit} of '{product.Name}'. Reason: {reason}");
            return (true, $"Damaged stock recorded. Current stock: {product.CurrentStock}");
        }

        public List<StockMovement> GetStockMovements(int? productId = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var db = new AppDbContext();
            var query = db.StockMovements
                .Include(m => m.Product)
                .AsNoTracking()
                .AsQueryable();

            if (productId.HasValue && productId.Value > 0)
            {
                query = query.Where(m => m.ProductId == productId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(m => m.MovementDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                DateTime end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(m => m.MovementDate <= end);
            }

            return query.OrderByDescending(m => m.MovementDate).ToList();
        }

        public (int TotalProducts, int LowStockCount, int TotalUnitsInStock, decimal TotalStockValuation) GetStockSummary()
        {
            using var db = new AppDbContext();
            var products = db.Products.AsNoTracking().Where(p => p.IsActive).ToList();

            int totalProducts = products.Count;
            int lowStock = products.Count(p => p.TrackStock && p.CurrentStock <= p.MinimumStockLevel);
            int totalUnits = products.Where(p => p.TrackStock).Sum(p => p.CurrentStock);
            decimal valuation = products.Where(p => p.TrackStock).Sum(p => p.CurrentStock * p.PurchasePrice);

            return (totalProducts, lowStock, totalUnits, valuation);
        }
    }
}
