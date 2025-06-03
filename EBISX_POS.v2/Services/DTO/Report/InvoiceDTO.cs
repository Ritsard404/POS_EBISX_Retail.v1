using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Services.DTO.Report
{
    public class InvoiceDTO
    {
        public long InvoiceNum { get; set; } = 0;
        public string InvoiceNumString { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string CashierEmail { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public string InvoiceStatus { get; set; } = string.Empty;

    }
}
