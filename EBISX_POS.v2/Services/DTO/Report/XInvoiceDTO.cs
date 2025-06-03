using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Services.DTO.Report
{

    public class Payment
    {
        public string CashString { get; set; }
        public List<OtherPayment> OtherPayments { get; set; }
        public string Total { get; set; }
    }

    public class OtherPayment
    {
        public string Name { get; set; }
        public string AmountString { get; set; }
    }

    public class TransactionSummary
    {
        public string CashInDrawer { get; set; }
        public List<OtherPayment> OtherPayments { get; set; }
    }

    public class XInvoiceDTO
    {
        public string BusinessName { get; set; }
        public string OperatorName { get; set; }
        public string AddressLine { get; set; }
        public string VatRegTin { get; set; }
        public string Min { get; set; }
        public string SerialNumber { get; set; }
        public string ReportDate { get; set; }
        public string ReportTime { get; set; }
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string Cashier { get; set; }
        public string BeginningOrNumber { get; set; }
        public string EndingOrNumber { get; set; }
        public string TransactCount { get; set; }
        public string OpeningFund { get; set; }
        public Payment Payments { get; set; }
        public string VoidAmount { get; set; }
        public string VoidCount { get; set; }
        public string Refund { get; set; }
        public string RefundCount { get; set; }
        public string Withdrawal { get; set; }
        public TransactionSummary TransactionSummary { get; set; }
        public string ShortOver { get; set; }
    }

}
