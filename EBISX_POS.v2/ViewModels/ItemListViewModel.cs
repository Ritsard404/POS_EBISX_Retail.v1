using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using EBISX_POS.API.Models; // Ensure this is added
using EBISX_POS.Services;
using System.Threading.Tasks; // Ensure this is added
using System.Diagnostics; // Ensure this is added
using System.Linq;

namespace EBISX_POS.ViewModels
{
    public partial class ItemListViewModel : ViewModelBase
    {
        private readonly MenuService _menuService;
        private ObservableCollection<ItemMenu> _allMenuItems = new();
        private string _searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ItemMenu> _filteredMenuItems = new();

        public ICommand ItemClickCommand { get; }
        public ICommand SearchCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                // Only update the property, don't trigger filtering automatically
                SetProperty(ref _searchText, value);
            }
        }

        public ItemListViewModel(MenuService menuService) 
        {
            _menuService = menuService;
            ItemClickCommand = new RelayCommand<ItemMenu>(OnItemClick);
            SearchCommand = new RelayCommand(PerformSearch);
        }

        // This property returns true if there are any menus.
        public bool HasMenus => FilteredMenuItems.Count > 0;

        public async Task LoadMenusAsync(int categoryId)
        {
            var menus = await _menuService.GetMenusAsync(categoryId);
            _allMenuItems.Clear();
            menus.ForEach(menu => _allMenuItems.Add(menu));
            FilterItems();

            // Notify that HasMenus may have changed.
            OnPropertyChanged(nameof(HasMenus));
        }

        private void PerformSearch()
        {
            FilterItems();
        }

        private void FilterItems()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredMenuItems = new ObservableCollection<ItemMenu>(_allMenuItems);
            }
            else
            {
                var filtered = _allMenuItems
                    .Where(item => item.ItemName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                FilteredMenuItems = new ObservableCollection<ItemMenu>(filtered);
            }
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
