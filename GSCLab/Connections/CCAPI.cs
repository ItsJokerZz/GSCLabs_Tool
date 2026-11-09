using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Schema;
using GSCLab.Infrastructure;
using GSCLab.Views;
using PS3Lib;
using PS3Lib.NET;

namespace GSCLab.Connections
{
    public static class CCAPI
    {
        private static PS3API API;
        public static bool CCAPIConnected;

        public static bool Connect(string ipAddress)
        {
            if (CCAPIConnected)
                return true;

            API = new PS3API(SelectAPI.ControlConsole);

            try
            {
                if (API.ConnectTarget(ipAddress))
                {
                    if (API.AttachProcess())
                    {
                        //API.CCAPI.Notify(PS3Lib.CCAPI.NotifyIcon.INFO, "GSCLabs: Connected & Attached");
                        //CCAPIConnected = true;
                        return true;
                    }
                    else
                    {
                        //CCAPIConnected = false;
                        ToastNotification.Show("Error: PS3 Connected But Unable To Attach To Process", true);
                        return false;
                    }
                }
                else
                {
                    //CCAPIConnected = false;
                    ToastNotification.Show("Error: Connecting To PS3", true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                //CCAPIConnected = false;
                _ = Discord.SendDiscordError(Session.Username + " PS3 Connection Error : " + ex);
                ToastNotification.Show("Error: Connecting To PS3", true);
                return false;
            }
        }

        public static void ReAttachProcess()
        {
            if (API != null)
            {
                API.AttachProcess();
                //API.CCAPI.Notify(PS3Lib.CCAPI.NotifyIcon.INFO, "GSCLabs: Reattached");
            }
        }

        public static void WriteUInt(uint pAddress, uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            API.SetMemory(pAddress, bytes);
        }

        public static void WriteString(uint pAddress, string value)
        {
            API.SetMemory(pAddress, Encoding.UTF8.GetBytes(value));
        }

        public static void SetMemory(uint offset, byte[] buffer)
        {
            if (API != null)
            {
                byte[] sizeBytes = BitConverter.GetBytes(buffer.Length);
                API.SetMemory(0x10040000, sizeBytes);
                API.SetMemory(0x10040004, buffer);
                API.CCAPI.Notify(PS3Lib.CCAPI.NotifyIcon.INFO, "Project Injected");
            }
            else
            {
                throw new Exception("Cannot set memory. PS3 is not connected or attached.");
            }
        }
    }
}