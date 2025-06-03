using Avalonia.Controls;
using EBISX_POS.API.Services.Interfaces;
using EBISX_POS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS.v2.Views
{
    public partial class PosTerminalInfoView : Window
    {
        public PosTerminalInfoView()
        {
            InitializeComponent();

            // Get the data service from DI container
            var ebisxService = App.Current.Services.GetRequiredService<IEbisxAPI>();

            // Create and set the ViewModel
            DataContext = new PosTerminalInfoViewModel(ebisxService, this);
        }
    }
} 