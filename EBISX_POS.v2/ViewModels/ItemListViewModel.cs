using CommunityToolkit.Mvvm.Input;
using EBISX_POS.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using EBISX_POS.API.Models; // Ensure this is added
using EBISX_POS.Services;
using System.Threading.Tasks; // Ensure this is added
using System.Diagnostics; // Ensure this is added

namespace EBISX_POS.ViewModels
{
    public class ItemListViewModel : ViewModelBase
    {
        private readonly MenuService _menuService;

        public ObservableCollection<ItemMenu> MenuItems { get; } = new();
        public ICommand ItemClickCommand { get; }

        public ItemListViewModel(MenuService menuService) 
        {
            _menuService = menuService;
            ItemClickCommand = new RelayCommand<ItemMenu>(OnItemClick);

        }

        // This property returns true if there are any menus.
        public bool HasMenus => MenuItems.Count > 0;

        public async Task LoadMenusAsync(int categoryId)
        {
            var menus = await _menuService.GetMenusAsync(categoryId);
            MenuItems.Clear();
            menus.ForEach(menu => MenuItems.Add(menu));

            // Notify that HasMenus may have changed.
            OnPropertyChanged(nameof(HasMenus));
        }

        private void OnItemClick(ItemMenu? item)
        {
            if (item != null)
            {
                Debug.WriteLine($"Clicked: {item.Id}, Price: {item.Price}"); // Debugging
            }
        }
    }
}
