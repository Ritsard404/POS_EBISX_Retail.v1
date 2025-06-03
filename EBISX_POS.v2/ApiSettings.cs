using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS
{
    public class ApiSettings
    {
        public required OnlineAPI OnlineAPI { get; set; }
        public required LocalAPI LocalAPI { get; set; }
    }

    public class OnlineAPI
    {
        public required string BaseUrl { get; set; }
    }

    public class LocalAPI
    {
        public required string BaseUrl { get; set; }
        public required string AuthEndpoint { get; set; }
        public required string MenuEndpoint { get; set; }
        public required string OrderEndpoint { get; set; }
        public required string PaymentEndpoint { get; set; }
        public required string ReportEndpoint { get; set; }
    }

    

}
