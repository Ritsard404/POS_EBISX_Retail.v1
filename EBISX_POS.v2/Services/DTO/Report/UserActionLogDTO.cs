using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Services.DTO.Report
{
    public class UserActionLogDTO
    {
        public string? Name { get; set; }
        public string? ManagerEmail { get; set; }
        public string? CashierName { get; set; }
        public string? CashierEmail { get; set; }
        public string? Amount { get; set; }

        public required string Action { get; set; }
        public required string ActionDate { get; set; }
    }
}
