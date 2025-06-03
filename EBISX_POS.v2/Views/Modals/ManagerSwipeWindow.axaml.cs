using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using EBISX_POS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS.Views
{
    public partial class ManagerSwipeWindow : Window
    {
        private readonly TaskCompletionSource<(bool success, string email)> _completionSource = new();
        private readonly AuthService _authService;
        private Window? MainAppWindow =>
            (Application.Current.ApplicationLifetime
                 as IClassicDesktopStyleApplicationLifetime)?
                    .MainWindow;

        public ManagerSwipeWindow(string header, string message, string ButtonName)
        {
            InitializeComponent();
            DataContext = this;

            // Get AuthService from DI container
            _authService = App.Current.Services.GetRequiredService<AuthService>();

            // Find controls
            HeaderTextBlock = this.FindControl<TextBlock>("HeaderTextBlock");
            BodyMessageTextBlock = this.FindControl<TextBlock>("BodyMessageTextBlock");
            SwipeButton = this.FindControl<Button>("SwipeButton");
            EmailTextBox = this.FindControl<TextBox>("EmailTextBox");
            ValidationTextBlock = this.FindControl<TextBlock>("ValidationTextBlock");

            // Debugging - Check if controls are found
            if (HeaderTextBlock == null)
            {
                Console.WriteLine("? HeaderTextBlock is NULL! Check XAML x:Name.");
            }
            else
            {
                HeaderTextBlock.Text = header;
            }

            if (BodyMessageTextBlock == null)
            {
                Console.WriteLine("? BodyMessageTextBlock is NULL! Check XAML x:Name.");
            }
            else
            {
                BodyMessageTextBlock.Text = message;
            }

            if (SwipeButton == null)
            {
                Console.WriteLine("? SwipeButton is NULL! Check XAML x:Name.");
            }
            else
            {
                SwipeButton.Content = ButtonName;
            }

            if (EmailTextBox == null)
            {
                Console.WriteLine("? EmailTextBox is NULL! Check XAML x:Name.");
            }

            if (ValidationTextBlock == null)
            {
                Console.WriteLine("? ValidationTextBlock is NULL! Check XAML x:Name.");
            }

            Opened += (sender, e) =>
            {
                // Focus the EmailTextBox when the window opens
                EmailTextBox?.Focus();
            };

            // Close the dialog automatically after 5 seconds if no action is taken
            DispatcherTimer.RunOnce(async () =>
            {
                if (!_completionSource.Task.IsCompleted)
                {
                    try
                    {
                        _completionSource.SetResult((false, string.Empty));
                        Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing window: {ex.Message}");
                    }
                }
            }, TimeSpan.FromSeconds(10));
        }

        public async Task<(bool success, string email)> ShowDialogAsync(Window? owner = null)
        {
            try
            {
                // pick either the passed‑in owner or your MainWindow:
                var dialogOwner = owner ?? MainAppWindow;

                if (dialogOwner == null || !dialogOwner.IsVisible)
                    return (false, string.Empty);

                // Show the dialog
                await ShowDialog(dialogOwner);

                // Wait for the completion source
                return await _completionSource.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ShowDialogAsync: {ex.Message}");
                return (false, string.Empty);
            }
        }

        private async void OnSwipeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (EmailTextBox == null || ValidationTextBlock == null) return;

            try
            {
                var email = EmailTextBox.Text?.Trim() ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowValidationError("Please enter a manager email");
                    return;
                }

                // Validate the manager email
                var (isValid, message) = await _authService.IsManagerValid(email);
                
                if (!isValid)
                {
                    ShowValidationError("Invalid Manager Email!");
                    return;
                }

                // Clear any validation message
                ValidationTextBlock.IsVisible = false;
                
                // Set the result with the email and close the window
                _completionSource.SetResult((true, email));
                Debug.WriteLine($"Manager Email: {email}, Success: true");
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during validation: {ex.Message}");
                ShowValidationError("An error occurred during validation. Please try again.");
            }
        }

        private void ShowValidationError(string message)
        {
            if (ValidationTextBlock == null) return;
            
            ValidationTextBlock.Text = message;
            ValidationTextBlock.IsVisible = true;
        }
    }
}
