using System;
using System.Linq;
using LMS.Models;
using LMS.Services;
using Xunit;

namespace LMS.Tests
{
    public class InventoryAndProductTests : TestBase
    {
        private readonly AuditService _auditService = new AuditService();
        private readonly InventoryService _inventoryService;
        private readonly ProductService _productService;

        public InventoryAndProductTests()
        {
            _inventoryService = new InventoryService(_auditService);
            _productService = new ProductService(_auditService);
        }

        [Fact]
        public void Product_CreationAndOpeningStock_GeneratesStockMovement()
        {
            var product = new Product
            {
                ProductCode = "PRD-MOB-01",
                Barcode = "8901234567890",
                Name = "Redmi Note 13",
                Category = "Mobile",
                Brand = "Xiaomi",
                Model = "Note 13",
                PurchasePrice = 45000,
                CashSalePrice = 52000,
                InstallmentSalePrice = 58000,
                CurrentStock = 10,
                MinimumStockLevel = 3,
                TrackStock = true,
                Unit = "Pcs"
            };

            var saveRes = _productService.SaveProduct(product, 1, "Admin");
            Assert.True(saveRes.Success);
            Assert.True(product.Id > 0);

            // Verify opening movement was created
            var movements = _inventoryService.GetStockMovements(product.Id);
            Assert.Single(movements);
            Assert.Equal(StockMovementType.OpeningStock, movements[0].MovementType);
            Assert.Equal(10, movements[0].Quantity);
            Assert.Equal(45000, movements[0].UnitPrice);
        }

        [Fact]
        public void StockPurchasesAndDeductions_MaintainAccurateStockLedger()
        {
            var product = new Product
            {
                ProductCode = "PRD-APPL-01",
                Name = "Inverter AC 1.5 Ton",
                Category = "Appliances",
                PurchasePrice = 110000,
                CashSalePrice = 135000,
                CurrentStock = 5,
                MinimumStockLevel = 2,
                TrackStock = true
            };
            _productService.SaveProduct(product, 1, "Admin");

            // 1. Purchase additional 10 units
            var purRes = _inventoryService.RecordPurchase(product.Id, 10, 112000, "PUR-101", "Batch 2 purchase", 1, "Admin");
            Assert.True(purRes.Success);

            var updatedProd = _productService.GetProductById(product.Id);
            Assert.Equal(15, updatedProd!.CurrentStock);
            Assert.Equal(112000, updatedProd.PurchasePrice);

            // 2. Sale deduction of 3 units
            var saleRes = _inventoryService.RecordSaleDeduction(product.Id, 3, "INV-2026-0001", 1, "Admin");
            Assert.True(saleRes.Success);

            updatedProd = _productService.GetProductById(product.Id);
            Assert.Equal(12, updatedProd!.CurrentStock);

            // 3. Customer returns 1 unit
            var retRes = _inventoryService.RecordReturn(product.Id, 1, "INV-2026-0001", "Customer swap", 1, "Admin");
            Assert.True(retRes.Success);

            updatedProd = _productService.GetProductById(product.Id);
            Assert.Equal(13, updatedProd!.CurrentStock);

            // 4. Record 1 damaged unit
            var dmgRes = _inventoryService.RecordDamagedStock(product.Id, 1, "Broken during transit", 1, "Admin");
            Assert.True(dmgRes.Success);

            updatedProd = _productService.GetProductById(product.Id);
            Assert.Equal(12, updatedProd!.CurrentStock);

            // Verify all movements exist in history
            var movements = _inventoryService.GetStockMovements(product.Id);
            Assert.Equal(5, movements.Count); // Opening, Purchase, Sale, Return, Damaged
        }

        [Fact]
        public void LowStockProducts_IdentifiesProductsUnderThreshold()
        {
            var p1 = new Product { ProductCode = "PRD-LOW-1", Name = "Low Stock Item", CurrentStock = 1, MinimumStockLevel = 3, TrackStock = true };
            var p2 = new Product { ProductCode = "PRD-LOW-2", Name = "High Stock Item", CurrentStock = 20, MinimumStockLevel = 5, TrackStock = true };
            _productService.SaveProduct(p1, 1, "Admin");
            _productService.SaveProduct(p2, 1, "Admin");

            var lowList = _productService.GetLowStockProducts();
            Assert.Contains(lowList, p => p.ProductCode == "PRD-LOW-1");
            Assert.DoesNotContain(lowList, p => p.ProductCode == "PRD-LOW-2");
        }
    }
}
