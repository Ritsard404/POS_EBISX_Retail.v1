using System.Collections.ObjectModel;
using EBISX_POS.ViewModels; // Ensure this is added
using EBISX_POS.API.Models; // Ensure this is added
using EBISX_POS.State;
using System.Threading.Tasks;
using EBISX_POS.Services;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.Models;

namespace EBISX_POS.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly MenuService _menuService; 
        private readonly ConnectivityViewModel _connectivity;


        [ObservableProperty]
        private bool isOnline;

        [ObservableProperty]
        private bool isTrainMode = CashierState.IsTrainMode;

        public ObservableCollection<Category> ButtonList { get; } = new();
        public OrderSummaryViewModel OrderSummaryViewModel { get; } // Add this property
        public ItemListViewModel ItemListViewModel { get; } // Add this property

        public MainWindowViewModel(MenuService menuService)
        {
            _menuService = menuService;
            OrderSummaryViewModel = new OrderSummaryViewModel(); // Initialize it
            ItemListViewModel = new ItemListViewModel(menuService); // Initialize it

            _connectivity = App.Current.Services.GetRequiredService<ConnectivityViewModel>();


            // Prime and subscribe to online/offline
            StartConnectivityTracking();

            _ = LoadCategories();
            _ = LoadPendingOrder();

        }

        public string CashierName => CashierState.CashierName ?? "Developer";

        private async Task LoadCategories()
        {
            var categories = await _menuService.GetCategoriesAsync();
            ButtonList.Clear();
            categories.ForEach(category => ButtonList.Add(category));

            int categoryId = categories?.Select(c => c.Id).FirstOrDefault() ?? 0;
            await LoadMenusAsync(categoryId);

        }
        private async Task LoadPendingOrder()
        {
            var orderService = App.Current.Services.GetRequiredService<OrderService>();

            // Fetch the pending orders (grouped by EntryId) from the API.
            var ordersDto = await orderService.GetCurrentOrderItems();
            var eligiblePwdScNames = await orderService.GetElligiblePWDSCDiscount();

            // If the items collection has empty items, exit.
            if (!ordersDto.Any() && !eligiblePwdScNames.Any())
                return;

            OrderState.CurrentOrder.Clear();
            foreach (var dto in ordersDto)
            {
                // Map the DTO's SubOrders to an ObservableCollection<SubOrderItem>
                var subOrders = new ObservableCollection<SubOrderItem>(
                    dto.SubOrders.Select(s => new SubOrderItem
                    {
                        MenuId = s.MenuId,
                        DrinkId = s.DrinkId,
                        AddOnId = s.AddOnId,
                        Name = s.Name,
                        ItemPrice = s.ItemPrice,
                        Size = s.Size,
                        Quantity = s.Quantity,
                        IsFirstItem = s.IsFirstItem,
                        IsOtherDisc = s.IsOtherDisc
                    })
                );

                // Create a new OrderItemState from the DTO.
                var pendingItem = new OrderItemState()
                {
                    ID = dto.EntryId,             // Using EntryId from the DTO.
                    Quantity = dto.TotalQuantity, // Total quantity from the DTO.
                    TotalPrice = dto.TotalPrice,  // Total price from the DTO.
                    HasCurrentOrder = dto.HasCurrentOrder,
                    SubOrders = subOrders,         // Mapped sub-orders.
                    HasDiscount = dto.HasDiscount,
                    IsEnableEdit = !dto.HasDiscount,
                    TotalDiscountPrice = dto.DiscountAmount,
                    IsPwdDiscounted = dto.IsPwdDiscounted,
                    IsSeniorDiscounted = dto.IsSeniorDiscounted,
                    PromoDiscountAmount = dto.PromoDiscountAmount,
                    HasPwdScDiscount = dto.HasDiscount && dto.PromoDiscountAmount == null,
                    CouponCode = dto.CouponCode,
                    IsVatExempt = dto.IsVatExempt
                };

                // Add the mapped OrderItemState to the static collection.
                OrderState.CurrentOrder.Add(pendingItem);
            }

            // Refresh UI display (if needed by your application).
            OrderState.CurrentOrderItem.RefreshDisplaySubOrders();

            TenderState.ElligiblePWDSCDiscount = eligiblePwdScNames;
        }

        private void StartConnectivityTracking()
        {
            // 1) prime
            IsOnline = _connectivity.IsOnline;

            // 2) subscribe
            _connectivity.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConnectivityViewModel.IsOnline))
                    IsOnline = _connectivity.IsOnline;
            };

            // 3) start monitoring (guarded inside ConnectivityViewModel)
            _ = _connectivity.StartMonitoringCommand.ExecuteAsync(null);
        }

        public async Task LoadMenusAsync(int categoryId)
        {
            await ItemListViewModel.LoadMenusAsync(categoryId);
        }
    }
}
