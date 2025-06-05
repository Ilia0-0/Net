using System;
using System.IO;
using System.Threading;

class NetworkMonitor
{
    static (long bytesIn, long bytesOut) GetNetworkStats(string interfaceName)
    {
        long totalBytesIn = 0;
        long totalBytesOut = 0;

        if (!File.Exists("/proc/net/dev"))
        {
            throw new FileNotFoundException("Файл /proc/net/dev не найден. Убедитесь, что вы на Linux-системе.");
        }

        string[] lines = File.ReadAllLines("/proc/net/dev");
        bool interfaceFound = false;

        foreach (var line in lines)
        {
            if (line.Contains("Inter-") || line.Contains("face") || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 10) continue;

            string currentInterface = parts[0].Trim(':');
            if (currentInterface != interfaceName) continue;

            interfaceFound = true;
            totalBytesIn = long.Parse(parts[1]);
            totalBytesOut = long.Parse(parts[9]);
        }

        if (!interfaceFound)
        {
            throw new Exception($"Интерфейс {interfaceName} не найден. Проверьте доступные интерфейсы с помощью команды 'ip link'.");
        }

        return (totalBytesIn, totalBytesOut);
    }

    static void PrintNetworkGraph(double speedIn, double speedOut)
    {
        Console.WriteLine("\n--- Сетевой граф (ASCII) ---");
        Console.WriteLine("wlo1");
        Console.WriteLine("|-- Входящий трафик: " + speedIn.ToString("F2") + " KB/s");
        Console.WriteLine("|-- Исходящий трафик: " + speedOut.ToString("F2") + " KB/s");
        Console.WriteLine("---------------------------\n");
    }

    static void Main()
    {
        Console.WriteLine("Мониторинг сетевого трафика (Ctrl+C для завершения)...");

        try
        {
            string networkInterface = "wlo1";
            Console.WriteLine($"Мониторинг интерфейса: {networkInterface}");

            var (prevIn, prevOut) = GetNetworkStats(networkInterface);
            int iteration = 0;

            while (true)
            {
                Thread.Sleep(1000);
                var (currIn, currOut) = GetNetworkStats(networkInterface);

                double speedIn = (currIn < prevIn) ? 0 : (currIn - prevIn) / 1024.0;
                double speedOut = (currOut < prevOut) ? 0 : (currOut - prevOut) / 1024.0;

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Входящий трафик: {speedIn:F2} KB/s");
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} - Исходящий трафик: {speedOut:F2} KB/s");

                // Печать ASCII-графика каждые 5 итераций
                if (iteration % 5 == 0)
                {
                    PrintNetworkGraph(speedIn, speedOut);
                }

                prevIn = currIn;
                prevOut = currOut;
                iteration++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Console.WriteLine("Нажмите Enter для выхода...");
            Console.ReadLine();
        }
    }
}
