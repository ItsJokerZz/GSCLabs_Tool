using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GSCLab.Infrastructure;
using JRPC_Client;
using XDevkit;
using System.IO;
using GSCLab.Views;

namespace GSCLab.Connections
{
    public static class Xbox
    {
        private static IXboxConsole JTAG;
        public static bool isJTAG;

        public static void xbConnect()
        {
            JTAG.Connect(out JTAG);

            if(JTAG.IsConnected())
            {
                isJTAG = true;
            }
            else
            {
                isJTAG = false;
            }
        }

        public static void xbLoadXeX()
        {
            if(JTAG.IsConnected())
            {
                try
                {
                    int num = (int)JTAG.Call<uint>("xboxkrnl.exe", 409, new object[4]
                    {
                        (object) ("Hdd:\\GSCLabs.xex"),
                        (object) 8,
                        (object) 0,
                        (object) 0
                    });
                }
                catch (Exception e) { _ = Discord.SendDiscordError("Error: " + Session.Username + " XEX Loading Error | " + e.ToString() ); }
            }
            else
            {
                GscMessageBox.Show("Console Not Connected");
            }
        }
    }
}
