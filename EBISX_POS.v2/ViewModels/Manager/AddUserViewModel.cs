using System;
using System.Threading.Tasks;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using Tmds.DBus.Protocol;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class AddUserViewModel : ViewModelBase
    {
        private readonly IData _dataService;
        private readonly Window _window;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _role = string.Empty;

        [ObservableProperty]
        private string? _errorMessage;
        public ObservableCollection<string> AvailableRoles { get; } =
    new ObservableCollection<string> { "Manager", "Cashier" };

        public AddUserViewModel(IData dataService, Window window)
        {
            _dataService = dataService;
            _window = window;
        }

        [RelayCommand]
        private async Task AddUser()
        {
            Debug.WriteLine("→ AddUser started");
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Role))
            {
                ShowError("All fields are required");
                return;
            }

            try
            {
                var newUser = new User
                {
                    UserEmail = Email,
                    UserFName = FirstName,
                    UserLName = LastName,
                    UserRole = Role,
                };

                var (isSuccess, message, _) = await _dataService.AddUser(newUser, CashierState.ManagerEmail!);

                if (isSuccess)
                {
                    _window.Close();
                }
                else
                {
                    ShowError(message);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to add user: {ex.Message}");
                Debug.WriteLine($"❌ AddUser exception: {ex}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _window.Close();
        }

        private void ShowError(string message)
        {
            var msgBox = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Error",
                    ContentMessage = message,
                    Icon = Icon.Error
                });

            msgBox.ShowAsPopupAsync(_window);
        }

    }
}