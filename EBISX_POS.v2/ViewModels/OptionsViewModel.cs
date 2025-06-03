using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.Models;
using EBISX_POS.Services.DTO.Menu;
using EBISX_POS.State;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EBISX_POS.ViewModels
{
    public partial class OptionsViewModel : ViewModelBase
    {
        public bool HasDrinks => OptionsState.DrinkTypes.Any();
        public bool HasAddOns => OptionsState.AddOnsType.Any();


        public ObservableCollection<DrinkTypeDTO> DrinkTypes { get; } = OptionsState.DrinkTypes;
        public ObservableCollection<DrinkDetailDTO> Drinks { get; } = OptionsState.Drinks;
        public ObservableCollection<string> DrinkSizes { get; } = OptionsState.DrinkSizes;
        public ObservableCollection<AddOnTypeDTO> AddOnsType { get; } = OptionsState.AddOnsType;
        public ObservableCollection<AddOnDetailDTO> AddOns { get; } = OptionsState.AddOns;

        public OptionsViewModel()
        {

            OptionsState.DrinkTypes.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasDrinks));
            };

            OptionsState.AddOnsType.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasAddOns));
            };

            if (DrinkTypes.Any() && DrinkSizes.Any())
            {
                OptionsState.UpdateDrinks(DrinkTypes.FirstOrDefault().DrinkTypeId, DrinkSizes.FirstOrDefault());
            }

            if (AddOnsType.Any())
            {
                OptionsState.UpdateAddOns(AddOnsType.FirstOrDefault().AddOnTypeId);
            }
        }
    }
}
