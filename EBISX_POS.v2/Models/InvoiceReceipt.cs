using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Models
{
    public class InvoiceReceipt
    {
        // Business Information
        public string BusinessName { get; set; }
        public string Operator { get; set; }
        public string Address { get; set; }
        public string VATRegTIN { get; set; }
        public string MIN { get; set; }
        public string SerialNumber { get; set; }
        public string ReportType { get; set; } // X-READING or Z-READING

        // Report Metadata
        public string ReportDate { get; set; }
        public string ReportTime { get; set; }
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string Cashier { get; set; }

        // Invoice Numbers
        public int BeginningSI { get; set; }
        public int EndingSI { get; set; }
        public int BeginningVOID { get; set; }
        public int EndingVOID { get; set; }
        public int BeginningRETURN { get; set; }
        public int EndingRETURN { get; set; }
        public int ResetCounter { get; set; }
        public int ZCounter { get; set; }

        // Sales Summary
        public decimal PresentAccumulatedSales { get; set; }
        public decimal PreviousAccumulatedSales { get; set; }
        public decimal SalesForTheDay { get; set; }

        // Breakdown of Sales
        public decimal VatableSales { get; set; }
        public decimal VatAmount { get; set; }
        public decimal VatExemptSales { get; set; }
        public decimal ZeroRatedSales { get; set; }

        // Net Sales Calculation
        public decimal GrossAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal VoidAmount { get; set; }
        public decimal VatAdjustment { get; set; }
        public decimal NetAmount { get; set; }

        // Discount Summary
        public decimal SCDiscount { get; set; }
        public decimal PWDDiscount { get; set; }
        public decimal NAACDiscount { get; set; }
        public decimal SoloParentDiscount { get; set; }
        public decimal OtherDiscount { get; set; }

        // Sales Adjustments
        public decimal SalesVoid { get; set; }
        public decimal SalesReturn { get; set; }

        // VAT Adjustments
        public decimal SCTransaction { get; set; }
        public decimal PWDTransaction { get; set; }
        public decimal RegDiscTransaction { get; set; }
        public decimal ZeroRatedTransaction { get; set; }
        public decimal VatOnReturn { get; set; }
        public decimal OtherVatAdjustments { get; set; }

        // Payments Received
        public decimal CashReceived { get; set; }
        public decimal ChequeReceived { get; set; }
        public decimal CreditCardReceived { get; set; }
        public decimal GiftCertificate { get; set; }
        public decimal TotalPayments { get; set; }

        // Transaction Summary
        public decimal CashInDrawer { get; set; }
        public decimal OpeningFund { get; set; }
        public decimal WithdrawalAmount { get; set; }   
        public decimal ShortOver { get; set; }


        public DateTimeOffset DateIssued { get; set; }
        public DateTimeOffset ValidUntil { get; set; }
        public DateTimeOffset DateCreated { get; set; }


        //Transaction log
        public string TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal TransactTotalAmount { get; set; }
        public string ReceiptContent { get; set; }  // e.g., a formatted string or HTML

    }
}
