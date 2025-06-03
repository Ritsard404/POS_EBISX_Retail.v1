namespace EBISX_POS
{
    /// <summary>
    /// Configuration settings for sales report paths
    /// </summary>
    public class SalesReport
    {
        /// <summary>
        /// Path for storing receipt files
        /// </summary>
        public required string Receipts { get; set; }

        /// <summary>
        /// Path for storing searched invoice files
        /// </summary>
        public required string SearchedInvoice { get; set; }

        /// <summary>
        /// Path for storing daily sales report files
        /// </summary>
        public required string DailySalesReport { get; set; }

        /// <summary>
        /// Path for storing X invoice report files
        /// </summary>
        public required string XInvoiceReport { get; set; }

        /// <summary>
        /// Path for storing Z invoice report files
        /// </summary>
        public required string ZInvoiceReport { get; set; }

        /// <summary>
        /// Path for storing cash track report files
        /// </summary>
        public required string CashTrackReport { get; set; }

        /// <summary>
        /// Path for storing transaction log files
        /// </summary>
        public required string TransactionLogs { get; set; }
        public required string TransactionLogsFolder { get; set; }
        public required string AuditTrailFolder { get; set; }
    }
}
