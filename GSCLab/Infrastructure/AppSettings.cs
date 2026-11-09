using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GSCLab.Infrastructure
{
    public static class AppSettings
    {
        private static string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GSCLab");
        private static string file = Path.Combine(folder, "config.dat");

        public static void Save(string user, string pass, bool remember)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string raw = $"{user}|{pass}|{remember}";
            byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(raw), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(file, encrypted);
        }

        public static (string user, string pass, bool remember) Load()
        {
            if (!File.Exists(file)) return ("", "", false);
            try
            {
                byte[] encrypted = File.ReadAllBytes(file);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                string[] parts = Encoding.UTF8.GetString(decrypted).Split('|');
                return (parts[0], parts[1], bool.Parse(parts[2]));
            }
            catch { return ("", "", false); }
        }
    }
}
