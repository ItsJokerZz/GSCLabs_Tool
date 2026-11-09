using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GSCLab.Infrastructure
{
    public static class ServerProvider
    {
        private static readonly HttpClient client;

        static ServerProvider()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMinutes(2);
        }

    public class GameMetadata
    {
        public string game { get; set; }
        public List<string> methods { get; set; }
        public List<string> functions { get; set; }
        public List<string> tokens { get; set; }
    }
}
}