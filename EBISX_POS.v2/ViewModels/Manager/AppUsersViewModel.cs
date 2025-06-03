using System.Collections.ObjectModel;
using System.Threading.Tasks;
using EBISX_POS.API.Models;
using EBISX_POS.API.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Linq;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace EBISX_POS.ViewModels.Manager
{
    public partial class AppUsersViewModel : ViewModelBase
    {
        private readonly IData _dataService;
        private readonly Window _window;

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        private User? _selectedUser;

        [ObservableProperty]
        private bool _isLoading;

        public List<string> Roles { get; } = new() { "Manager", "Cashier" };
        public List<string> StatusOptions { get; } = new() { "Active", "InActive" };

        public AppUsersViewModel(IData dataService, Window window)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _window = window ?? throw new ArgumentNullException(nameof(window));

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                await LoadUsersCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing AppUsersViewModel: {ex}");
            }
        }

        [RelayCommand]
        private async Task LoadUsers()
        {
            try
            {
                //await Dispatcher.UIThread.InvokeAsync(() =>
                //{
                //    IsLoading = true;
                //    ErrorMessage = null;
                //});

                var users = await _dataService.GetUsers();

                //await Dispatcher.UIThread.InvokeAsync(() =>
                //{
                Users = new ObservableCollection<User>(users);
                //});
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading users: {ex}");
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        [RelayCommand]
        private async Task SaveUserChanges(User user)
        {
            if (user == null) return;

            try
            {
                var (isSuccess, message) = await _dataService.UpdateUser(user, CashierState.ManagerEmail!);

                if (isSuccess)
                {
                    await LoadUsersCommand.ExecuteAsync(null);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving user changes: {ex}");
            }
        }

        [RelayCommand]
        private async Task AddUser()
        {
            try
            {
                var addUserWindow = new AddUserWindow();
                addUserWindow.DataContext = new AddUserViewModel(_dataService, addUserWindow);

                await addUserWindow.ShowDialog(_window);
                await LoadUsersCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening add user window: {ex}");
            }
        }

        [RelayCommand]
        private async Task CloseWindow()
        {
            _window.Close();
        }

        partial void OnSelectedUserChanged(User? value)
        {
            if (value != null)
            {
                SaveUserChangesCommand.Execute(value);
            }
        }
    }
}