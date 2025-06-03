// ConnectivityViewModel.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EBISX_POS.ViewModels
{
    public partial class ConnectivityViewModel : ObservableObject
    {
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        private bool _isOnline;

        public IAsyncRelayCommand StartMonitoringCommand { get; }
        public IRelayCommand StopMonitoringCommand { get; }

        public ConnectivityViewModel()
        {
            StartMonitoringCommand = new AsyncRelayCommand(StartMonitoringAsync);
            StopMonitoringCommand = new RelayCommand(StopMonitoring);
        }

        private async Task StartMonitoringAsync()
        {
            if (_cts != null) return; // already running

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    IsOnline = await NetworkHelper.IsOnlineAsync();
                    await Task.Delay(_pollInterval, token);
                }
            }
            catch (TaskCanceledException) { }
        }

        private void StopMonitoring()
        {
            _cts?.Cancel();
            _cts = null;
        }
    }
}