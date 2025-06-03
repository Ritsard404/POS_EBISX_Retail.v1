using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using EBISX_POS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS.Views
{
    public partial class LogInWindow : Window
    {
        public LogInWindow()
        {
            InitializeComponent();
            var viewModel = App.Current.Services.GetRequiredService<LogInWindowViewModel>();
            DataContext = viewModel;
            //Loaded += async (sender, e) => await viewModel.LoadCashiers();
        }
    }
}