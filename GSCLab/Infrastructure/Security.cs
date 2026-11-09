using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace GSCLab.Infrastructure
{
    public static class Security
    {
        public static string GetFingerprint()
        {
            string rawDataDan = GetCpuId() + GetMotherboardSerial() + GetDiskSerial_Dan();
            string rawData = GetCpuId() + GetMotherboardSerial() + GetDiskSerial();

            using (SHA256 sha256 = SHA256.Create())
            {

                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(Session.Username == "DanMillar99" ? rawDataDan : rawData));
                //byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawDataDan));


                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes) builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        private static string GetCpuId() => GetWmiProperty("Win32_Processor", "ProcessorId");

        private static string GetMotherboardSerial() => GetWmiProperty("Win32_BaseBoard", "SerialNumber");

        private static string GetDiskSerial_Dan()
        {
            try
            {
                string query = "SELECT SerialNumber FROM Win32_PhysicalMedia WHERE Tag LIKE '%PHYSICALDRIVE0%'";

                using (var searcher = new ManagementObjectSearcher(query))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string serial = obj["SerialNumber"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(serial)) return serial;
                    }
                }
            }
            catch
            {
                return GetFallbackDiskSerial();
            }
            return "UNKNOWN";
        }

        private static string GetFallbackDiskSerial()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive"))
            {
                foreach (var obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString()?.Trim() ?? "UNKNOWN";
                }
            }
            return "UNKNOWN";
        }

        private static string GetDiskSerial()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string serial = obj["SerialNumber"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(serial)) return serial;
                    }
                }
            }
            catch
            {
            }
            return "UNKNOWN";
        }

        private static string GetWmiProperty(string wmiClass, string property)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj[property]?.ToString().Trim() ?? "NONE";
                    }
                }
            }
            catch { return "ERROR"; }
            return "UNKNOWN";
        }
    }
}