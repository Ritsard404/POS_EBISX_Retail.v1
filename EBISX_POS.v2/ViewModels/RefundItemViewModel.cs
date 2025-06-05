using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EBISX_POS.ViewModels
{
    public partial class RefundItemViewModel : ViewModelBase
    {
        private readonly IOrder _orderService;
        private readonly Window _window;

        [ObservableProperty]
        private bool _isSelectingItems = false;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _invoice;

        [ObservableProperty]
        private ObservableCollection<Item> _selectedItems = new();

        [ObservableProperty]
        private ObservableCollection<Item> _items = new();

        public RefundItemViewModel(IOrder orderService, Window window)
        {
            _orderService = orderService;
            _window = window;
        }

        [RelayCommand]
        public async Task SearchId()
        {
            IsLoading = true;

            if (!long.TryParse(Invoice, out var invoiceNum) || string.IsNullOrEmpty(Invoice))
            {
                // Parsing failed → show an error and bail out
                var parseErrorBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Error",
                    ContentMessage = "Invoice must be a valid number.",
                    Icon = Icon.Error
                });

                await parseErrorBox.ShowAsPopupAsync(_window);
                IsLoading = false;
                _window.Close();
                return;
            }

            var items = await _orderService.GetItems(invoiceNum);
            if (items != null && items.Any())
            {
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }

                IsSelectingItems = true;
            }
            else
            {
                var msgBox = MessageBoxManager
                    .GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "Error",
                        ContentMessage = "Invalid invoice!",
                        Icon = Icon.Error
                    });

                await msgBox.ShowAsPopupAsync(_window);
                _window.Close();

            }

            IsLoading = false;
        }
        [RelayCommand]
        public async Task VerifyManagerEmail()
        {
            IsLoading = true;
            var swipeManager = new ManagerSwipeWindow(header: "Manager", message: "Please ask the manager to enter email.", ButtonName: "Refund");
            var (success, email) = await swipeManager.ShowDialogAsync(_window);

            if (success)
            {
                await RefundSelectedItems(email);
                _window.Close();
            }
            IsLoading = false;
            _window.Close();
        }

        public async Task RefundSelectedItems(string managerEmail)
        {
            IsLoading = true;

            long invoiceNum = long.Parse(Invoice);

            var (isSuccess, messsage) = await _orderService.RefundItemOrder(managerEmail, invoiceNum, SelectedItems.ToList());
            if (isSuccess)
            {
                var msgBox = MessageBoxManager
                    .GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "Refunded",
                        ContentMessage = "Items Refunded!",
                        Icon = Icon.Success
                    });

                await msgBox.ShowAsPopupAsync(_window);
                _window.Close();
            }

            IsLoading = false;
            IsSelectingItems = false;
        }
    }
}
