using Avalonia.Controls;
using Avalonia.Interactivity;
using EBISX_POS.Models;
using EBISX_POS.State;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace EBISX_POS.Views
{
    public partial class SelectDiscountPwdScWindow : Window, INotifyPropertyChanged
    {
        // Backing field for a single selection.
        // false means Senior is selected (the default), true means PWD is selected.
        private bool _isPwdSelected = false;

        // Public property for PWD selection.
        public bool IsPwdSelected
        {
            get => _isPwdSelected;
            set
            {
                if (_isPwdSelected != value)
                {
                    _isPwdSelected = value;
                    OnPropertyChanged(nameof(IsPwdSelected));
                    // Also update the complementary property.
                    OnPropertyChanged(nameof(IsSeniorSelected));
                }
            }
        }

        // Derived property: true when Senior is selected (i.e. when IsPwdSelected is false).
        public bool IsSeniorSelected
        {
            get => !_isPwdSelected;
            set
            {
                // When binding sets IsSeniorSelected, update IsPwdSelected accordingly.
                // If Senior is set to true, then PWD is false, and vice versa.
                if (value != (!_isPwdSelected))
                {
                    IsPwdSelected = !value;
                }
            }
        }
        private int _maxSelectionCount = 0;
        public int MaxSelectionCount
        {
            get => _maxSelectionCount;
            set
            {
                if (_maxSelectionCount != value)
                {
                    _maxSelectionCount = value;
                    OnPropertyChanged(nameof(MaxSelectionCount));
                }
            }
        }

        // New property for the total selected count.
        private int _totalSelected;
        public int TotalSelected
        {
            get => _totalSelected;
            set
            {
                if (_totalSelected != value)
                {
                    _totalSelected = value;
                    OnPropertyChanged(nameof(TotalSelected));
                }
            }
        }

        private bool _isAdjustingSelection;

        public string SelectedIDsDebug
            => SelectedIDs?.Any() == true
               ? string.Join(", ", SelectedIDs)
               : "(none)";

        // New property for the selected IDs.
        private List<string> _selectedIDs = new List<string>();
        public List<string> SelectedIDs
        {
            get => _selectedIDs;
            set
            {
                if (_selectedIDs != value)
                {
                    _selectedIDs = value;
                    OnPropertyChanged(nameof(SelectedIDs));

                    OnPropertyChanged(nameof(SelectedIDsDebug));
                    Debug.WriteLine($"[DEBUG] Selected IDs: {SelectedIDsDebug}");
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public SelectDiscountPwdScWindow()
        {

            InitializeComponent();
            DataContext = this;
            this.Opened += OnWindowOpened;
        }

        private void OnWindowOpened(object? sender, System.EventArgs e)
        {
            RefreshCurrentOrder();
        }

        private void RefreshCurrentOrder()
        {
            CurrentOrder.ItemsSource = OrderState.CurrentOrder
                .Where(d => !d.HasDiscount)
                .GroupBy(i => i.ID)
                .Select(g => g.First());
        }

        private void EditQuantity_Click(object? sender, RoutedEventArgs e)
        {
            // Retrieve the CommandParameter from the sender (a Button in this example).
            // Alternatively, if you attach the parameter directly from XAML, you can also use e.Parameter.
            if (sender is Button button && button.CommandParameter is object parameter)
            {
                int intDelta = 0;

                // If parameter is an integer.
                if (parameter is int directValue)
                {
                    intDelta = directValue;
                }
                // If it's a string, try to parse it.
                else if (parameter is string s && int.TryParse(s, out int parsedValue))
                {
                    intDelta = parsedValue;
                }
                else
                {
                    return;
                }


                // Calculate the new max selection count.
                int newMax = MaxSelectionCount + intDelta;
                if (newMax < 0)
                    newMax = 0;  // Ensure it doesn't go negative

                // Clamp newMax so that it doesn't exceed the total available items.
                int totalAvailable = OrderState.CurrentOrder.Count;
                if (newMax > totalAvailable)
                    newMax = totalAvailable;

                // If the new max is less than the current total selected, remove the excess selections.
                if (newMax < TotalSelected)
                {
                    // Calculate how many items need to be removed.
                    int excessCount = TotalSelected - newMax;
                    // Remove items from the SelectedItems collection.
                    var itemsToRemove = CurrentOrder.SelectedItems
                                        .Cast<OrderItemState>()
                                        .Take(excessCount)
                                        .ToList();
                    foreach (var item in itemsToRemove)
                    {
                        CurrentOrder.SelectedItems.Remove(item);
                    }
                }

                // Finally, update the MaxSelectionCount property.
                MaxSelectionCount = newMax;
            }
        }

        // This event is triggered whenever the selection changes in the ListBox.
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAdjustingSelection)
                return;

            var listBox = (ListBox)sender;
            var selected = listBox.SelectedItems.Cast<OrderItemState>().ToList();


            if (selected.Count > MaxSelectionCount)
            {
                _isAdjustingSelection = true;

                // keep only the first MaxSelectionCount items
                var allowed = selected.Take(MaxSelectionCount).ToList();

                listBox.SelectedItems.Clear();
                foreach (var item in allowed)
                    listBox.SelectedItems.Add(item);

                _isAdjustingSelection = false;
            }

            // Get the total selected count.
            TotalSelected = listBox.SelectedItems.Count;

            // Extract the IDs from the selected items.
            SelectedIDs = listBox.SelectedItems
                .Cast<OrderItemState>()
                .Select(item => item.ID)
                .ToList();
        }

        private async void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            if (SelectedIDs.Count() == 0)
            {
                Close();
                return;
            }
            if (SelectedIDs.Count() > MaxSelectionCount || SelectedIDs.Count() < MaxSelectionCount)
            {
                return;
            }

            var promoWindow = new GetSeniorPwdInfo(SelectedIDs: SelectedIDs, IsPwdSelected: IsPwdSelected, inputCount: MaxSelectionCount);
            await promoWindow.ShowDialog(this);

            Close();
        }
    }
}
