using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSCLab.Infrastructure
{
    public static class Session
    {
        public static string Username { get; set; }
        public static string Token { get; set; }
        public static string Role { get; set; }
        public static double Stars { get; set; }
        public static bool IsTester { get; set; }
        public static string GithubUsername { get; set; }
        public static string EncryptedGithubToken { get; set; }
    }
}
