using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IoFileTestCLIEnv;

class Program
{
    // ÁLLAPOT VÁLTOZÓK (State Management)
    // 1. A megtalált eszközök listája
    static List<DeviceModel> discoveredDevices = new List<DeviceModel>();

    // 2. A kért változók a kapcsolódáshoz
    static bool connectedToHost = false;     // Van-e élő kapcsolat?
    static string connectedIp = string.Empty; // Kihez kapcsolódtunk?
    static string connectedName = string.Empty; // Csak a kényelem kedvéért

    static async Task Main(string[] args)
    {
        string myName = "PC-" + Guid.NewGuid().ToString().Substring(0, 4);
        var discoveryService = new NetworkDiscoveryService(myName);

        // FELIRATKOZÁS: Ha jön új eszköz, betesszük a listába
        discoveryService.OnDeviceFound += (device) =>
        {
            // Lockoljuk a listát, hogy a háttérszál és a főszál ne vesszen össze
            lock (discoveredDevices)
            {
                // Csak akkor adjuk hozzá, ha még nincs benne (IP alapján)
                if (!discoveredDevices.Any(d => d.IpAddress == device.IpAddress))
                {
                    discoveredDevices.Add(device);

                    // Ha nem vagyunk épp menüben, jelezzük a felhasználónak
                    if (!connectedToHost)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[!] ÚJ ESZKÖZ: {device.Name} ({device.IpAddress})");
                        Console.ResetColor();
                        PrintPrompt();
                    }
                }
            }
        };

        // Figyelés indítása
        var listenerTask = discoveryService.StartListeningAsync();

        // FŐ CIKLUS
        while (true)
        {
            Console.Clear();
            PrintHeader(myName);

            // Ha csatlakozva vagyunk, mást mutatunk
            if (connectedToHost)
            {
                PrintConnectedMenu();
            }
            else
            {
                PrintDiscoveryMenu();
            }

            PrintPrompt();
            var keyInfo = Console.ReadKey(true);

            // Gombnyomások kezelése
            if (connectedToHost)
            {
                // --- CSATLAKOZTATOTT MÓD ---
                switch (keyInfo.Key)
                {
                    case ConsoleKey.D: // Disconnect
                        Disconnect();
                        break;
                    case ConsoleKey.Q: // Quit
                        return;
                }
            }
            else
            {
                // --- KERESÉS MÓD ---
                switch (keyInfo.Key)
                {
                    case ConsoleKey.B: // Broadcast
                        Console.WriteLine("\n [~] Keresés indítása...");
                        discoveredDevices.Clear(); // Tiszta lappal indulunk
                        await discoveryService.SendDiscoveryBroadcastAsync();
                        // Várunk kicsit, hogy beérkezzenek a válaszok
                        await Task.Delay(1000);
                        break;

                    case ConsoleKey.L: // Listázás & Csatlakozás
                        ConnectToDeviceUI();
                        break;

                    case ConsoleKey.Q: // Quit
                        return;
                }
            }
        }
    }

    // --- SEGÉDFÜGGVÉNYEK ---

    static void PrintHeader(string myName)
    {
        Console.WriteLine("=============================================");
        Console.WriteLine($" IoFiles Console v0.2 - {myName}");
        Console.WriteLine("=============================================");
    }

    static void PrintDiscoveryMenu()
    {
        Console.WriteLine(" ÁLLAPOT: Nincs kapcsolat (Idle)");
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine(" [B] -> Broadcast (Keresés újra)");
        Console.WriteLine(" [L] -> Lista megtekintése & Csatlakozás");
        Console.WriteLine(" [Q] -> Kilépés");
        Console.WriteLine("---------------------------------------------");
    }

    static void PrintConnectedMenu()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($" ÁLLAPOT: KAPCSOLÓDVA IDE: {connectedName}");
        Console.WriteLine($" IP CÍM: {connectedIp}");
        Console.ResetColor();
        Console.WriteLine("---------------------------------------------");
        Console.WriteLine(" [F] -> Fájl küldése (Még nincs kész)");
        Console.WriteLine(" [D] -> Kapcsolat bontása (Disconnect)");
        Console.WriteLine(" [Q] -> Kilépés");
        Console.WriteLine("---------------------------------------------");
    }

    static void PrintPrompt()
    {
        if (connectedToHost) Console.Write("CONNECTED > ");
        else Console.Write("MENU > ");
    }

    // A csatlakozás logikája
    static void ConnectToDeviceUI()
    {
        Console.WriteLine("\n\n--- ELÉRHETŐ ESZKÖZÖK ---");

        lock (discoveredDevices)
        {
            if (discoveredDevices.Count == 0)
            {
                Console.WriteLine(" Nincs találat. Nyomj 'B'-t a kereséshez!");
                Console.WriteLine(" Nyomj egy gombot a visszalépéshez...");
                Console.ReadKey(true);
                return;
            }

            for (int i = 0; i < discoveredDevices.Count; i++)
            {
                Console.WriteLine($" [{i}] {discoveredDevices[i].Name} - {discoveredDevices[i].IpAddress}");
            }

            Console.WriteLine("-------------------------");
            Console.Write(" Írd be a számot a csatlakozáshoz (vagy 'x' mégse): ");
            string input = Console.ReadLine();

            if (int.TryParse(input, out int index) && index >= 0 && index < discoveredDevices.Count)
            {
                // SIKERES CSATLAKOZÁS
                var target = discoveredDevices[index];

                connectedToHost = true;       // Változó beállítása
                connectedIp = target.IpAddress; // IP mentése
                connectedName = target.Name;

                // Itt majd később elindíthatjuk a TCP kapcsolatot is
            }
        }
    }

    static void Disconnect()
    {
        connectedToHost = false;
        connectedIp = string.Empty;
        connectedName = string.Empty;
        Console.WriteLine("\n Kapcsolat bontva.");
        // Itt majd le kell zárni a TCP socketet is
        System.Threading.Thread.Sleep(1000);
    }
}