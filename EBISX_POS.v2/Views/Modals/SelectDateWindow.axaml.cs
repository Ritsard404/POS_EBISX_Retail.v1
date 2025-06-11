using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;

namespace EBISX_POS.Views
{
    public partial class SelectDateWindow : Window
    {
        private readonly TaskCompletionSource<DateTime?> _completionSource = new();

        public SelectDateWindow()
        {
            InitializeComponent();

            var picker = this.FindControl<DatePicker>("DatePickerControl");
            picker.SelectedDate = DateTime.Today;
        }

        public async Task<DateTime?> ShowDialogAsync(Window owner, DateTime? defaultDate = null)
        {
            // Do not create a new instance here � use 'this'
            if (defaultDate.HasValue)
            {
                var picker = this.FindControl<DatePicker>("DatePickerControl");
                picker.SelectedDate = defaultDate.Value;
            }

            await this.ShowDialog(owner); // Show current window
            return await _completionSource.Task;
        }

        private void OnSelectButtonClick(object? sender, RoutedEventArgs e)
        {
            var picker = this.FindControl<DatePicker>("DatePickerControl");
            var selectedDate = picker.SelectedDate;

            if (selectedDate.HasValue)
            {
                _completionSource.TrySetResult(selectedDate.Value.LocalDateTime);
            }
            else
            {
                _completionSource.TrySetResult(null);
            }

            Close();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (!_completionSource.Task.IsCompleted)
                _completionSource.TrySetResult(null);

            base.OnClosing(e);
        }
    }
}
