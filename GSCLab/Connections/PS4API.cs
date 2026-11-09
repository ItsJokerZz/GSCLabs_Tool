using GSCLab.Infrastructure;
using GSCLab.Views;
using NexusFramework;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace GSCLab.Connections
{
    public static class PS4API
    {
        private const int GAME_PORT = 6969;
        private const int ALLOC_PORT = 6767;
        private const int QUERY_TIMEOUT_MS = 1200;
        private const int GAME_CACHE_MS = 1500;
        private const string SPRX_PATH = "/data/GSCLabs/GSCLabs.sprx";
        private const string ERROR_TITLE = "An Error Has Occurred";

        private static readonly Library nexus = new();
        private static readonly SemaphoreSlim gate = new(1, 1);

        public static bool isConnected = false;

        private static string currentGame = string.Empty;
        private static string currentSession = string.Empty;
        private static ulong currentAllocAddr = ulong.MinValue;
        private static long lastQueryTicks = 0;

        private static readonly Dictionary<string, string> GameNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["iw6ps4"] = "Ghosts",
            ["s1ps4"] = "Advanced Warfare",
            ["t7ps4"] = "Black Ops 3",
            ["iw7ps4"] = "Infinite Warfare",
            ["h1ps4"] = "Modern Warfare Remastered",
            ["s2ps4"] = "World War 2",
            ["t8ps4"] = "Black Ops 4",
            ["iw8ps4"] = "Modern Warfare (2019)",
            ["h2ps4"] = "Modern Warfare 2 CR",
            ["t9ps4"] = "Cold War",
            ["s4ps4"] = "Vanguard",
            ["iw9ps4"] = "Modern Warfare 2 (2022)",
            ["jupps4"] = "Modern Warfare 3 (2023)",
            ["t10ps4"] = "Black Ops 6 (ALPHA)",
        };

        private static void Log(string detail, Exception? ex = null)
        {
            _ = Discord.SendDiscordError("[NexusFramework] " + detail + (ex == null ? string.Empty : "\n" + ex));
        }

        private static void ShowError(string message)
        {
            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.Invoke(() => GscMessageBox.Show(message, ERROR_TITLE));
            else
                GscMessageBox.Show(message, ERROR_TITLE);
        }

        private static string FriendlyName(string game)
        {
            return GameNames.TryGetValue(game, out string? name) ? name : game;
        }

        private static void ResetState()
        {
            currentGame = string.Empty;
            currentSession = string.Empty;
            currentAllocAddr = ulong.MinValue;
            lastQueryTicks = 0;
        }

        public static async Task SendPayload(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return;

            isConnected = false;
            ResetState();

            bool sent = false;

            try
            {
                await nexus.SendPayload(ipAddress);
                sent = await nexus.IsRunning();
            }
            catch (Exception ex)
            {
                Log($"SendPayload failed for {ipAddress}", ex);
            }

            ToastNotification.Show(sent
                ? "NexusFramework payload sent successfully!"
                : "Failed to send payload, check the IP and try again!");
        }

        public static async Task Connect(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return;

            isConnected = false;
            ResetState();

            try
            {
                await nexus.Connect(ipAddress);
                isConnected = nexus.Target.Connected;
            }
            catch (Exception ex)
            {
                Log($"Connect failed for {ipAddress}", ex);
            }

            if (!isConnected)
                ShowError("Failed to connect, make sure NexusFramework is running!");
        }

        public static void Disconnect()
        {
            isConnected = false;
            ResetState();
        }

        private static async Task<string> QueryPort(int port)
        {
            if (!isConnected)
                return string.Empty;

            using CancellationTokenSource cts = new(QUERY_TIMEOUT_MS);

            try
            {
                using TcpClient client = new();
                client.NoDelay = true;

                await client.ConnectAsync(nexus.Target.IP, port, cts.Token);
                client.ReceiveTimeout = 10000;

                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII);

                return (await reader.ReadLineAsync().WaitAsync(cts.Token) ?? string.Empty).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static async Task<bool> RefreshGame(bool force)
        {
            if (!force && currentGame.Length > 0 && Environment.TickCount64 - lastQueryTicks < GAME_CACHE_MS)
                return true;

            string line = await QueryPort(GAME_PORT);
            lastQueryTicks = Environment.TickCount64;

            if (line.Length == 0 || line.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            {
                ResetState();
                return false;
            }

            string game = line;
            string session = string.Empty;

            int sep = line.IndexOf(':');
            if (sep >= 0)
            {
                game = line.Substring(0, sep).Trim();
                session = line.Substring(sep + 1).Trim();
            }

            if (!game.EndsWith("ps4", StringComparison.OrdinalIgnoreCase))
                game += "ps4";

            if (!string.Equals(game, currentGame, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(session, currentSession, StringComparison.Ordinal))
            {
                currentGame = game;
                currentSession = session;
                currentAllocAddr = ulong.MinValue;
            }

            return true;
        }

        private static async Task<bool> EnsureReady(bool force)
        {
            if (!isConnected || !nexus.Target.Connected)
            {
                isConnected = false;
                ResetState();
                return false;
            }

            if (!await RefreshGame(force))
            {
                try
                {
                    await nexus.LoadModule(SPRX_PATH);
                }
                catch (Exception ex)
                {
                    Log($"LoadModule failed for {SPRX_PATH}", ex);
                    return false;
                }

                await Task.Delay(300);

                if (!await RefreshGame(true))
                {
                    Log($"SPRX loaded but silent on port {GAME_PORT}");
                    return false;
                }
            }

            if (currentAllocAddr != ulong.MinValue)
                return true;

            string text = await QueryPort(ALLOC_PORT);

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (!ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong address) || address == 0)
            {
                Log($"bad allocation address '{text}' for {currentGame}");
                return false;
            }

            currentAllocAddr = address;
            return true;
        }

        public static async Task<byte[]?> ReadMemory(ulong address, uint length)
        {
            if (!isConnected)
                return null;

            try
            {
                return await nexus.ReadMemory<byte[]>(address, length);
            }
            catch (Exception ex)
            {
                Log($"ReadMemory failed at 0x{address:X} ({length} bytes)", ex);
                return null;
            }
        }

        public static async Task<bool> WriteBytes(ulong address, byte[] bytes)
        {
            if (!isConnected)
                return false;

            try
            {
                await nexus.WriteMemory(address, bytes);
                return true;
            }
            catch (Exception ex)
            {
                Log($"WriteMemory failed at 0x{address:X} ({bytes.Length} bytes)", ex);
                return false;
            }
        }

        public static void xNotify(string message)
        {
            if (!isConnected)
                return;

            try
            {
                _ = nexus.SendNotification(message);
            }
            catch { /* DO NOTHING */ }
        }

        public static async Task<bool> IsOnGame(string game)
        {
            await gate.WaitAsync();

            try
            {
                if (await EnsureReady(false) && string.Equals(currentGame, game, StringComparison.OrdinalIgnoreCase))
                    return true;

                ShowError($"Please make sure '{FriendlyName(game)}' is running!");
                return false;
            }
            finally
            {
                gate.Release();
            }
        }

        public static async Task CompileProject(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return;

            await gate.WaitAsync();

            try
            {
                if (!await EnsureReady(true))
                {
                    ShowError("Please make sure your game is running!");
                    return;
                }

                ulong address = currentAllocAddr;

                if (!await WriteBytes(address, new byte[4]) ||
                    !await WriteBytes(address + 4, payload) ||
                    !await WriteBytes(address, BitConverter.GetBytes(payload.Length)))
                {
                    currentAllocAddr = ulong.MinValue;
                    Log($"payload write failed at 0x{address:X} ({payload.Length} bytes) on {currentGame}");
                    ShowError("Injection failed!");
                    return;
                }

                xNotify("Project Injected!");
            }
            finally
            {
                gate.Release();
            }
        }
    }
}