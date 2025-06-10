namespace EBISX_POS.Library.Services.DTO.Journal
{
    public class PushAccountJournalDTO
    {
        public string Entry_Type { get; set; } = "INVOICE";
        public string Entry_No { get; set; }
        public string Entry_Line_No { get; set; }
        public string Entry_Date { get; set; }
        public string CostCenter { get; set; }
        public string ItemId { get; set; }
        public string Unit { get; set; }
        public string Qty { get; set; }
        public string Cost { get; set; }
        public string Price { get; set; }
        public string TotalPrice { get; set; }
        public string Debit { get; set; }
        public string Credit { get; set; }
        public string AccountBalance { get; set; }
        public string Prev_Reading { get; set; }
        public string Curr_Reading { get; set; }
        public string Memo { get; set; }
        public string AccountName { get; set; }
        public string Reference { get; set; }
        public string Entry_Name { get; set; }
        public string Cashier { get; set; }
        public string Count_Type { get; set; }
        public string Deposited { get; set; }
        public string Deposit_Date { get; set; }
        public string Deposit_Reference { get; set; }
        public string Deposit_By { get; set; }
        public string Deposit_Time { get; set; }
        public string CustomerName { get; set; }
        public string SubTotal { get; set; }
        public string TotalTax { get; set; }
        public string GrossTotal { get; set; }
        public string Discount_Type { get; set; }
        public string Discount_Amount { get; set; }
        public string NetPayable { get; set; }
        public string Status { get; set; }
        public string User_Email { get; set; }
        public string QtyPerBaseUnit { get; set; }
        public string QtyBalanceInBaseUnit { get; set; }
    }
}
