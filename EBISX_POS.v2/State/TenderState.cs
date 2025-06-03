using EBISX_POS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EBISX_POS.State
{
    public static class TenderState
    {
        public static TenderOrder tenderOrder = new TenderOrder();
        public static List<string>? ElligiblePWDSCDiscount { get; set; }

    }
}
