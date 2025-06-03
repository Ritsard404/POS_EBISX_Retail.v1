using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EBISX_POS.Views
{
    public partial class ItemCardView : UserControl
    {
        public ItemCardView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
