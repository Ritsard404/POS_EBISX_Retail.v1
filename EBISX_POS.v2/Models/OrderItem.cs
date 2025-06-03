using EBISX_POS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.Models
{
    public class OrderItem
    {
        public int ID { get; set; }
        public int Quantity { get; set; }
        public List<SubOrder> SubOrder { get; set; }

        // Computed property to track the first item
        public List<SubOrder> DisplaySubOrders => SubOrder
            .Select((s, index) => new SubOrder
            {
                ID = s.ID,
                Name = s.Name,
                Price = s.Price,
                Size = s.Size,
                IsFirstItem = index == 0, // True for the first item
                Quantity = index == 0 ? Quantity : 0 // Only show Quantity for the first item
            }).ToList();
    }

    public class SubOrder
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string? Size { get; set; }

        public bool IsFirstItem { get; set; } = false; // Identify first item
        public int Quantity { get; set; } = 0; // Store Quantity for first item

        public string DisplayName => string.IsNullOrEmpty(Size) ? Name : $"{Name} ({Size})";

        //  Opacity Property (Replaces Converter)
        public double Opacity => IsFirstItem ? 1.0 : 0.0;
    }
}
