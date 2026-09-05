using System;
using LMS.Data;
using LMS.Models;

namespace LMS.Services
{
    public class TerminologyService
    {
        private readonly SettingService _settingService;

        public TerminologyService(SettingService settingService)
        {
            _settingService = settingService;
        }

        public BusinessType GetActiveBusinessType()
        {
            string modeStr = _settingService.GetSettingValue("Business.Type", "Mixed");
            if (Enum.TryParse<BusinessType>(modeStr, true, out var result))
            {
                return result;
            }
            return BusinessType.Mixed;
        }

        public string CustomerSingular => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Tenant",
            BusinessType.InstallmentRetail => "Customer",
            BusinessType.BNPL => "Client",
            BusinessType.GeneralReceivables => "Party",
            _ => "Customer / Tenant"
        };

        public string CustomerPlural => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Tenants",
            BusinessType.InstallmentRetail => "Customers",
            BusinessType.BNPL => "Clients",
            BusinessType.GeneralReceivables => "Parties",
            _ => "Customers / Tenants"
        };

        public string ItemSingular => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Property / Unit",
            BusinessType.InstallmentRetail => "Product",
            BusinessType.BNPL => "Product / Order",
            BusinessType.GeneralReceivables => "Item / Service",
            _ => "Product / Property"
        };

        public string ItemPlural => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Properties & Units",
            BusinessType.InstallmentRetail => "Products & Inventory",
            BusinessType.BNPL => "Products & Orders",
            BusinessType.GeneralReceivables => "Items & Khata",
            _ => "Products & Properties"
        };

        public string AgreementOrSale => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Rent Agreement",
            BusinessType.InstallmentRetail => "Installment Sale",
            BusinessType.BNPL => "BNPL Order",
            BusinessType.GeneralReceivables => "Credit Sale",
            _ => "Sale / Agreement"
        };

        public string AgreementOrSalePlural => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Rent Agreements",
            BusinessType.InstallmentRetail => "Installment Sales",
            BusinessType.BNPL => "BNPL Orders",
            BusinessType.GeneralReceivables => "Credit Sales",
            _ => "Sales & Agreements"
        };

        public string InstallmentOrRent => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Rent",
            BusinessType.InstallmentRetail => "Installment",
            BusinessType.BNPL => "Due Payment",
            BusinessType.GeneralReceivables => "Due Amount",
            _ => "Installment / Rent"
        };

        public string DownPaymentOrAdvance => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Security Deposit / Advance",
            BusinessType.InstallmentRetail => "Down Payment",
            BusinessType.BNPL => "Upfront Payment",
            BusinessType.GeneralReceivables => "Initial Payment",
            _ => "Down Payment / Advance"
        };

        public string RegisterTitle => GetActiveBusinessType() switch
        {
            BusinessType.PropertyRent => "Landlord Rent & Account Register",
            BusinessType.InstallmentRetail => "Installment & Receivables Register",
            BusinessType.BNPL => "BNPL & Credit Register",
            BusinessType.GeneralReceivables => "Khata & Receivables Register",
            _ => "Universal Installment & Rent Register"
        };
    }
}
