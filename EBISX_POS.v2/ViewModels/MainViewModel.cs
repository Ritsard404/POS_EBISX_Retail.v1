using EBISX_POS.Services; // Ensure this is added

namespace EBISX_POS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ItemListViewModel ItemList { get; }

        public MainViewModel(MenuService menuService) // Update constructor
        {
            ItemList = new ItemListViewModel(menuService); // Pass menuService
        }
    }
}
