using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Services.DTO.Payment;
using EBISX_POS.Services;
using EBISX_POS.State;
using EBISX_POS.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace EBISX_POS.Views
{
    public partial class AlternativePaymentsWindow : Window
    {
        public AlternativePaymentsWindow()
        {
            InitializeComponent();
            CreateFields();
        }

        private async void CreateFields()
        {
            var paymentService = App.Current.Services.GetRequiredService<PaymentService>();

            Fields.IsVisible = false;
            IsLoadMenu.IsVisible = true;

            var paymentMethods = await paymentService.SaleTypes();

            foreach (var method in paymentMethods)
            {
                // Vertical container to hold the label and input fields
                var container = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                // Payment method name at the top left
                var textBlock = new TextBlock
                {
                    Text = method.Name,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 5, 0),
                    FontWeight = Avalonia.Media.FontWeight.Bold
                };

                // Horizontal panel for the input fields
                var inputPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5
                };

                // Reference TextBox with floating watermark
                var textBoxReference = new TextBox
                {
                    Width = 200,
                    Name = $"Reference{method.Id}",
                    Watermark = "Reference",
                    UseFloatingWatermark = true,
                    Height = 40,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                // Amount TextBox with floating watermark
                var textBoxAmount = new TextBox
                {
                    Width = 200,
                    Name = $"Amount{method.Id}",
                    Watermark = "Amount",
                    UseFloatingWatermark = true,
                    Height = 40,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                // Attach TextInput event handler to filter for decimals with up to 3 decimals
                textBoxAmount.AddHandler(TextInputEvent, AmountTextBox_OnTextInput, RoutingStrategies.Tunnel);

                // Add input fields to the horizontal panel
                inputPanel.Children.Add(textBoxReference);
                inputPanel.Children.Add(textBoxAmount);

                // Add label and input panel to the container
                container.Children.Add(textBlock);
                container.Children.Add(inputPanel);

                // Add the container to the main StackPanel
                InputStackPanel.Children.Add(container);
            }

            Fields.IsVisible = true;
            IsLoadMenu.IsVisible = false;
        }

        // Event handler to ensure the text is a valid decimal with up to 3 decimal places
        private void AmountTextBox_OnTextInput(object sender, TextInputEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Use an empty string if Text is null to prevent NullReferenceException
                var currentText = textBox.Text ?? "";
                var newText = currentText.Insert(textBox.CaretIndex, e.Text);

                // Regex: any number of digits, optional decimal point with up to 3 digits
                if (!Regex.IsMatch(newText, @"^\d*(\.\d{0,3})?$"))
                {
                    e.Handled = true;
                }
            }
        }


        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            var dtos = new ObservableCollection<AddAlternativePaymentsDTO>();

            // Iterate over each container in InputStackPanel
            foreach (var child in InputStackPanel.Children)
            {
                if (child is StackPanel container && container.Children.Count >= 2)
                {
                    var saleTypeNameTextBlock = container.Children[0] as TextBlock;

                    // The second child is the horizontal panel with inputs
                    if (container.Children[1] is StackPanel inputPanel && inputPanel.Children.Count >= 2)
                    {
                        var textBoxReference = inputPanel.Children[0] as TextBox;
                        var textBoxAmount = inputPanel.Children[1] as TextBox;

                        if (textBoxReference != null && textBoxAmount != null)
                        {
                            bool isReferenceEmpty = string.IsNullOrWhiteSpace(textBoxReference.Text);
                            bool isAmountEmpty = string.IsNullOrWhiteSpace(textBoxAmount.Text);

                            // If only one field is filled, log an error and skip this method
                            if (isReferenceEmpty ^ isAmountEmpty)
                            {
                                Debug.WriteLine($"Validation error: Payment method (SaleTypeId {ExtractSaleTypeId(textBoxAmount.Name)}) has incomplete inputs.");
                                return;
                            }

                            // Skip if both fields are empty
                            if (isReferenceEmpty && isAmountEmpty)
                                continue;

                            // Both fields are provided, try parsing the amount
                            if (decimal.TryParse(textBoxAmount.Text, out var amount))
                            {
                                int saleTypeId = ExtractSaleTypeId(textBoxAmount.Name);

                                dtos.Add(new AddAlternativePaymentsDTO
                                {
                                    Reference = textBoxReference.Text,
                                    Amount = amount,
                                    SaleTypeId = saleTypeId,
                                    SaleTypeName = saleTypeNameTextBlock?.Text ?? string.Empty
                                });
                            }
                            else
                            {
                                Debug.WriteLine($"Validation error: Unable to parse amount '{textBoxAmount.Text}' for SaleTypeId {ExtractSaleTypeId(textBoxAmount.Name)}.");
                            }
                        }
                    }
                }
            }

            if (TenderState.tenderOrder.OtherPayments != null)
                TenderState.tenderOrder.OtherPayments.Clear();
            TenderState.tenderOrder.OtherPayments = dtos;
            Close();
        }

        // Helper method to extract SaleTypeId from the TextBox name (assumes format "Amount{method.Id}")
        private int ExtractSaleTypeId(string textBoxName)
        {
            if (textBoxName.StartsWith("Amount") && int.TryParse(textBoxName.Substring("Amount".Length), out int saleTypeId))
            {
                return saleTypeId;
            }
            return 0;
        }
    }
}
