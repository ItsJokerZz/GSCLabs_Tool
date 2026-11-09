using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GSCLab.Infrastructure
{
    public static class Discord
    {
        private static DiscordRpcClient _client;
        private static string _applicationId = "1459782553132339280";
        public static bool isEnabled;
        private static DateTime _startTime;
        private const string ClientID = "1496679247237091462";

        public static async Task SendDiscordError(string errorMessage)
        {
            string webhookUrl = "https://discord.com/api/webhooks/1470493579490099319/3uvIGnVz7wCZXB12OZuJiw-vWC5_aoyRiIyMJyIJMD-8YZ7T3F6J6ieg0KSKQ4ENRQYb";

            string jsonPayload = $"{{\"content\": \"**GSCLab Error Report**\\n**Error:** {errorMessage}\"}}";

            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                try
                {
                    await client.PostAsync(webhookUrl, content);
                }
                catch
                {
                }
            }
        }
        public static void Initialize()
        {
            if (_client != null && _client.IsInitialized) return;

            _client = new DiscordRpcClient(ClientID);

            _client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            _startTime = DateTime.UtcNow;

            _client.Initialize();

            SetIdlePresence();
        }
        public static void SetIdlePresence()
        {
            UpdatePresence("Idling", "No Project Open", "gsclab_logo", "GSCLab IDE");
        }
        public static void UpdatePresence(string state, string details, string largeImageKey = "gsclab_logo", string largeImageText = "GSCLab IDE", string smallImageKey = "code_icon", string smallImageText = "Writing GSC")
        {
            if (_client == null || !_client.IsInitialized) return;

            _client.SetPresence(new RichPresence()
            {
                State = state,
                Details = details,
                Timestamps = new Timestamps(_startTime),
                Assets = new Assets()
                {
                    LargeImageKey = largeImageKey,
                    LargeImageText = largeImageText,
                    SmallImageKey = smallImageKey, // Now dynamic
                    SmallImageText = smallImageText  // Now dynamic
                }
            });
        }
        public static void Shutdown()
        {
            if (_client != null)
            {
                _client.ClearPresence();
                _client.Dispose();
            }
        }

    }
}
