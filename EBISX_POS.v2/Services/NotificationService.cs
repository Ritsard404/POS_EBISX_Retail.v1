using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia;

namespace EBISX_POS.Services
{
    public static class NotificationService
    {
        public static async void NetworkIssueMessage(Window owner)
        {
            //var desktop = Application.Current?.ApplicationLifetime
            //                 as IClassicDesktopStyleApplicationLifetime;
            //var owner = desktop?.MainWindow as Window;

            var alertBox = MessageBoxManager.GetMessageBoxStandard(
                new MessageBoxStandardParams
                {
                    ContentTitle = "Connection Issue",
                    ContentMessage = "We’re having a little trouble connecting right now. Please check your internet connection and try again. Thanks for your patience!",
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    SystemDecorations = SystemDecorations.None,
                });
            Debug.WriteLine("Imong Api Goy Taronga!");

            await alertBox.ShowAsPopupAsync(owner);
        }
    }
}
