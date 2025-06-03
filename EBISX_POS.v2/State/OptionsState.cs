using EBISX_POS.Services.DTO.Menu;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace EBISX_POS.State
{
    public static class OptionsState
    {
        public static ObservableCollection<DrinkTypeDTO> DrinkTypes { get; set; } = new ObservableCollection<DrinkTypeDTO>();
        public static ObservableCollection<DrinkDetailDTO> Drinks { get; set; } = new ObservableCollection<DrinkDetailDTO>();
        public static ObservableCollection<string> DrinkSizes { get; set; } = new ObservableCollection<string>();
        public static ObservableCollection<AddOnTypeDTO> AddOnsType { get; set; } = new ObservableCollection<AddOnTypeDTO>();
        public static ObservableCollection<AddOnDetailDTO> AddOns { get; set; } = new ObservableCollection<AddOnDetailDTO>();

        public static void UpdateDrinks(int? drinkTypeId, string? size)
        {
            Drinks.Clear();
            var drinksEntry = DrinkTypes.FirstOrDefault(d => d.DrinkTypeId == drinkTypeId)?
                                .SizesWithPrices?.FirstOrDefault(w => w.Size == size)
                                ?.Drinks;
            if (drinksEntry != null)
            {
                foreach (var drink in drinksEntry)
                {
                    // Map DrinksDTO to DrinkDetailDTO.
                    Drinks.Add(new DrinkDetailDTO
                    {
                        MenuId= drink.MenuId,
                        MenuName = drink.MenuName,
                        MenuImagePath = drink.MenuImagePath,
                        MenuPrice = drink.MenuPrice,
                        Size = drink.Size,
                        IsUpgradeMeal = drink.IsUpgradeMeal
                    });
                }
            }
        }



        public static void UpdateAddOns(int addOnTypeId)
        {
            AddOns.Clear();
            var addOns = AddOnsType.FirstOrDefault(a => a.AddOnTypeId == addOnTypeId)?.AddOns;
            if (addOns != null)
            {
                foreach (var addOn in addOns)
                {
                    addOn.HasSize = !string.IsNullOrEmpty(addOn.Size);
                    AddOns.Add(addOn);
                    //AddOns.Add(new AddOnDetailDTO
                    //{
                    //    MenuId = addOn.MenuId,
                    //    MenuName = addOn.MenuName,
                    //    Size = addOn.Size,
                    //    Price = addOn.Price,
                    //    IsUpgradeMeal = addOn.IsUpgradeMeal,

                    //    MenuImagePath = addOn.MenuImagePath,
                    //});
                }
            }
        }
    }
}
