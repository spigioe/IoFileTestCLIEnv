using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Dialogs.Internal;

namespace IoFileTestCLIEnv
{
    public class MainViewModel : AvaloniaDialogsInternalViewModelBase // Avalonia ViewModelBase
    {
        private readonly NetworkDiscoveryService _discoveryService;

        // Ez a lista jelenik meg a képernyőn
        public ObservableCollection<DeviceModel> AvailableDevices { get; } = new();

        public MainViewModel()
        {
            _discoveryService = new NetworkDiscoveryService("Sajat_Laptop_Windows");

            // Feliratkozunk az eseményre: ha találunk valakit, betesszük a listába
            _discoveryService.OnDeviceFound += (device) =>
            {
                // Fontos: Ellenőrizzük, hogy benne van-e már, ne duplikáljunk
                var existing = AvailableDevices.FirstOrDefault(d => d.IpAddress == device.IpAddress);
                if (existing == null)
                {
                    // UI szálra kell tenni a módosítást!
                    // Avalonia-ban: Dispatcher.UIThread.Invoke(...)
                    AvailableDevices.Add(device);
                }
            };

            // Elindítjuk a figyelést a háttérben
            _discoveryService.StartListeningAsync();
        }

        // Ezt a parancsot hívja meg a "Keresés" gomb
        public async Task RefreshDevicesCommand()
        {
            AvailableDevices.Clear(); // Lista törlése új keresés előtt
            await _discoveryService.SendDiscoveryBroadcastAsync();
        }
    }
}
