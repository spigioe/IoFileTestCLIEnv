using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoFileTestCLIEnv
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    public class NetworkDiscoveryService
    {
        private const int Port = 54321; // Egy szabadon választott port
        private const string ProtocolId = "IOFILES_V1"; // Magic string
        private readonly string _myDeviceName;

        // Esemény, ha új eszközt találtunk (erre iratkozik fel a ViewModel)
        public event Action<DeviceModel> OnDeviceFound;

        private UdpClient _udpClient;
        private bool _isRunning;

        public NetworkDiscoveryService(string deviceName)
        {
            _myDeviceName = deviceName;
        }

        // 1. A figyelő indítása (Ezt az alkalmazás indulásakor hívod meg)
        public async Task StartListeningAsync()
        {
            _udpClient = new UdpClient();

            // Fontos: Engedélyezzük, hogy több program is használhassa a portot (fejlesztésnél hasznos)
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Port));
            _isRunning = true;

            try
            {
                while (_isRunning)
                {
                    // Várakozunk bejövő adatra
                    var result = await _udpClient.ReceiveAsync();
                    ProcessMessage(result);
                }
            }
            catch (Exception ex)
            {
                // Hibakezelés (pl. leállításnál dobhat hibát, azt elnyeljük)
                Console.WriteLine($"Listener hiba: {ex.Message}");
            }
        }

        // 2. A keresés indítása (Broadcast küldés)
        public async Task SendDiscoveryBroadcastAsync()
        {
            using var broadcastClient = new UdpClient();
            broadcastClient.EnableBroadcast = true;

            string message = $"{ProtocolId}|DISCOVER|{_myDeviceName}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Küldés a 255.255.255.255-re
            var endpoint = new IPEndPoint(IPAddress.Broadcast, Port);
            await broadcastClient.SendAsync(data, data.Length, endpoint);

            Console.WriteLine("Broadcast elküldve...");
        }

        // 3. Beérkező üzenetek feldolgozása
        private async void ProcessMessage(UdpReceiveResult result)
        {
            string message = Encoding.UTF8.GetString(result.Buffer);
            string[] parts = message.Split('|');
            string remoteIp = result.RemoteEndPoint.Address.ToString();

            // Saját magunkat kiszűrjük (ha van helyi IP ellenőrzés, azt itt kell finomítani)
            // De legegyszerűbb, ha a név alapján szűrünk kezdetben
            if (parts.Length < 3 || !parts[0].Equals(ProtocolId)) return;
            if (parts[2] == _myDeviceName) return; // Saját magunkat hallottuk

            string command = parts[1];
            string remoteName = parts[2];

            if (command == "DISCOVER")
            {
                // Valaki keres minket! -> Válaszolunk neki közvetlenül (Unicast)
                Console.WriteLine($"Keresés érkezett innen: {remoteName} ({remoteIp})");
                await SendResponseAsync(result.RemoteEndPoint);

                // Opcionális: Akitől a kérés jött, azt is felvehetjük a listánkra azonnal
                NotifyDeviceFound(remoteName, remoteIp);
            }
            else if (command == "RESPONSE")
            {
                // Válasz érkezett a keresésünkre! -> Hozzáadjuk a listához
                Console.WriteLine($"Eszköz találtam: {remoteName} ({remoteIp})");
                NotifyDeviceFound(remoteName, remoteIp);
            }
        }

        // Válasz küldése közvetlenül a keresőnek
        private async Task SendResponseAsync(IPEndPoint target)
        {
            using var responseClient = new UdpClient();
            string message = $"{ProtocolId}|RESPONSE|{_myDeviceName}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            await responseClient.SendAsync(data, data.Length, target);
        }

        private void NotifyDeviceFound(string name, string ip)
        {
            // Az UI szálon kell majd kezelni, de itt csak jelezzük az adatot
            OnDeviceFound?.Invoke(new DeviceModel
            {
                Name = name,
                IpAddress = ip,
                LastSeen = DateTime.Now
            });
        }
    }
}
