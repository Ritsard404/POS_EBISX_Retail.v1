using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Services.DTO.Report
{
    public class InvoiceDetailsDTO
    {
        // Business Details
        public required string PrintCount { get; set; }
        public required string RegisteredName { get; set; }
        public required string Address { get; set; }
        public required string VatTinNumber { get; set; }
        public required string MinNumber { get; set; }

        // Invoice Details
        public required string InvoiceNum { get; set; }
        public required string InvoiceDate { get; set; }
        public required string OrderType { get; set; }
        public required string CashierName { get; set; }

        // Items
        public required List<ItemDTO> Items { get; set; } = new();

        // Totals
        public required string TotalAmount { get; set; }
        public required string DiscountAmount { get; set; }
        public required string DueAmount { get; set; }
        public List<OtherPaymentDTO>? OtherPayments { get; set; } = new();
        public required string CashTenderAmount { get; set; }
        public required string TotalTenderAmount { get; set; }
        public required string ChangeAmount { get; set; }
        public required string VatExemptSales { get; set; }
        public required string VatSales { get; set; }
        public required string VatAmount { get; set; }

        public List<string> ElligiblePeopleDiscounts { get; set; } = new();

        // POS Details
        public required string PosSerialNumber { get; set; }
        public required string DateIssued { get; set; }
        public required string ValidUntil { get; set; }

        public static InvoiceDetailsDTO CreateEmpty()
        {
            return new InvoiceDetailsDTO
            {
                RegisteredName = string.Empty,
                Address = string.Empty,
                VatTinNumber = string.Empty,
                MinNumber = string.Empty,

                InvoiceNum = string.Empty,
                InvoiceDate = string.Empty,
                OrderType = string.Empty,
                CashierName = string.Empty,

                Items = new List<ItemDTO>(),

                TotalAmount = string.Empty,
                DiscountAmount = string.Empty,
                DueAmount = string.Empty,
                OtherPayments = new List<OtherPaymentDTO>(),
                CashTenderAmount = string.Empty,
                TotalTenderAmount = string.Empty,
                ChangeAmount = string.Empty,
                VatExemptSales = string.Empty,
                VatSales = string.Empty,
                VatAmount = string.Empty,
                PrintCount = string.Empty,

                ElligiblePeopleDiscounts = new List<string>(),

                PosSerialNumber = string.Empty,
                DateIssued = string.Empty,
                ValidUntil = string.Empty
            };
        }
    }

    public class ItemDTO
    {
        public required int Qty { get; set; }
        public List<ItemInfoDTO> itemInfos { get; set; } = new();

        public static ItemDTO CreateEmpty()
        {
            return new ItemDTO
            {
                Qty = 0,
                itemInfos = new List<ItemInfoDTO>()
            };
        }
    }

    public class ItemInfoDTO
    {
        public required string Description { get; set; }
        public required string Amount { get; set; }
        public bool IsFirstItem { get; set; }

        public static ItemInfoDTO CreateEmpty()
        {
            return new ItemInfoDTO
            {
                Description = string.Empty,
                Amount = string.Empty
            };
        }
    }

    public class OtherPaymentDTO
    {
        public required string SaleTypeName { get; set; }
        public required string Amount { get; set; }

        public static OtherPaymentDTO CreateEmpty()
        {
            return new OtherPaymentDTO
            {
                SaleTypeName = string.Empty,
                Amount = string.Empty
            };
        }
    }
}
