
using System.Collections.Generic;

namespace EBISX_POS.Services.DTO.Report
{

    public class SalesBreakdown
    {
        public string VatableSales { get; set; }
        public string VatAmount { get; set; }
        public string VatExemptSales { get; set; }
        public string ZeroRatedSales { get; set; }
        public string GrossAmount { get; set; }
        public string LessDiscount { get; set; }
        public string LessReturn { get; set; }
        public string LessVoid { get; set; }
        public string LessVatAdjustment { get; set; }
        public string NetAmount { get; set; }
    }

    public class DiscountSummary
    {
        public string SeniorCitizen { get; set; }
        public string SeniorCitizenCount { get; set; }
        public string Pwd { get; set; }
        public string PwdCount { get; set; }
        public string Other { get; set; }
        public string OtherCount { get; set; }
    }

    public class SalesAdjustment
    {
        public string Void { get; set; }
        public string VoidCount { get; set; }
        public string Return { get; set; }
        public string ReturnCount { get; set; }
    }

    public class VatAdjustment
    {
        public string ScTrans { get; set; }
        public string PwdTrans { get; set; }
        public string RegDiscTrans { get; set; }
        public string ZeroRatedTrans { get; set; }
        public string VatOnReturn { get; set; }
        public string OtherAdjustments { get; set; }
    }

    public class ZInvoiceDTO
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
        public string BeginningSI { get; set; }
        public string EndingSI { get; set; }
        public string BeginningVoid { get; set; }
        public string EndingVoid { get; set; }
        public string BeginningReturn { get; set; }
        public string EndingReturn { get; set; }
        public string TransactCount { get; set; }
        public string ResetCounter { get; set; }
        public string ZCounter { get; set; }
        public string PresentAccumulatedSales { get; set; }
        public string PreviousAccumulatedSales { get; set; }
        public string SalesForTheDay { get; set; }
        public SalesBreakdown SalesBreakdown { get; set; }
        public DiscountSummary DiscountSummary { get; set; }
        public SalesAdjustment SalesAdjustment { get; set; }
        public VatAdjustment VatAdjustment { get; set; }
        public TransactionSummary TransactionSummary { get; set; }
        public string OpeningFund { get; set; }
        public string Withdrawal { get; set; }
        public string PaymentsReceived { get; set; }
        public string ShortOver { get; set; }
    }
}
