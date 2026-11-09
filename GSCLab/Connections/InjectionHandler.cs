using GSCLab.Infrastructure;
using System.Windows;

namespace GSCLab.Connections
{
    public static class InjectionHandler
    {
        public static Dictionary<string, ulong> GameOffsets = new Dictionary<string, ulong>();

        private static ulong ReturnPS3Offset(string game)
        {
            switch (game.ToLower())
            {
                //GHOSTS PS3
                case "iw6ps3_mp": return 0x10040000;
                case "iw6ps3_zm": return 0x10040000;
                //AW PS3
                case "s1ps3_mp": return 0x10040000;
                case "s1ps3_zm": return 0x10040000;
                //MW3 PS3
                case "iw5ps3_mp": return 0x10040000;
                //360
                //Wii U
                //PC
                default: return 0x0000;
            }
        }

        public static async Task<(bool Success, string Message)> ProcessInjection(byte[] compiledBytes, string game, string mode)
        {
            EditorTheme.Load();
            var settings = EditorTheme.Current;

            try
            {

                string ps3Ip = settings.Ps4IP;
                string lookupKey = $"{game}_{mode}".ToLower();

                ulong ps3_offset = ReturnPS3Offset(lookupKey);
                if (game.ToLower().EndsWith("ps3"))
                {
                    if (ps3_offset == 0)
                        return (false, $"No offset found for {lookupKey}. Check your ReturnOffset list.");

                    switch (Infrastructure.ProjectManager.ActivePlatform)
                    {
                        case "hen":
                            bool isHenConnected = PS3MAPI.ConnectTarget(ps3Ip);
                            if (!isHenConnected)
                                return (false, "Could not connect to PS3 via PS3MAPI. Check IP and ensure PS3MAPI is running.");
                            bool isHenAttached = PS3MAPI.AttachProcess();
                            if (!isHenAttached)
                                return (false, "Could not attach to game process via PS3MAPI. make sure game is running and ensure PS3MAPI is running.");
                            PS3MAPI.SetMemory((uint)ps3_offset, compiledBytes);
                            break;
                        case "ccapi":
                            bool isConnected = CCAPI.Connect(ps3Ip);
                            if (!isConnected)
                                return (false, "Could not connect to PS3 via CCAPI. Check IP and ensure CCAPI is running.");
                            CCAPI.SetMemory((uint)ps3_offset, compiledBytes);
                            break;
                        case "tmpai":
                            //Add Later//
                            break;
                    }

                    return (true, "Injected Project PS3");
                }

                else if (game.ToLower().EndsWith("ps4") || game.ToLower().EndsWith("ps5") || game.ToLower().StartsWith("bo3_ps4"))
                {
                    await PS4API.Connect(settings.Ps4IP);

                    bool isGameLoaded = await PS4API.IsOnGame(game);

                    if (!isGameLoaded)
                        return (false, "Game not loaded!");

                    if (!PS4API.isConnected)
                        return (false, "No console connected. Please connect via Nexus or PS4Debug.");
                    else
                    {
                        await PS4API.CompileProject(compiledBytes);

                        return (true, "Injected project via " + (settings.UseNexus ? "Nexus" : "PS4Debug" + "!"));
                    }

                }

                return (false, $"Console target for '{game}' is not supported yet.");
            }
            catch (Exception ex)
            {
                return (false, $"Injection Error: {ex.Message}");
            }
        }

        public static async Task<(bool Success, string Message)> ProcessOldGameInjection(string console, string rawCode, uint address)
        {
            EditorTheme.Load();
            var settings = EditorTheme.Current;

            switch (Infrastructure.ProjectManager.ActivePlatform)
            {
                case "hen":
                    bool isHenConnected = PS3MAPI.ConnectTarget(settings.Ps4IP);
                    if (!isHenConnected)
                        return (false, "Could not connect to PS3 via PS3MAPI. Check IP and ensure PS3MAPI is running.");
                    bool isHenAttached = PS3MAPI.AttachProcess();
                    if (!isHenAttached)
                        return (false, "Could not attach to game process via PS3MAPI. make sure game is running and ensure PS3MAPI is running.");
                    PS3MAPI.WriteUInt(address, 0x12000000);
                    PS3MAPI.WriteString(0x12000000, rawCode);
                    break;
                case "ccapi":
                    bool isConnected = CCAPI.Connect(settings.Ps4IP);
                    if (!isConnected)
                        return (false, "Could not connect to PS3 via CCAPI. Check IP and ensure CCAPI is running.");
                    MessageBox.Show("Connected");
                    CCAPI.WriteUInt(address, 0x12000000);
                    CCAPI.WriteString(0x12000000, rawCode);
                    break;
                case "tmpai":
                    //Add Later//
                    break;
            }

            return (true, "Injected Project PS3");
        }

        public static async Task<(bool Success, string Message)> ProcessInjection(string shortGameName, string console, string mode, byte[] compiledBytes)
        {
            string targetPlatform = console.ToUpper();

            if (targetPlatform == "CCAPI" || targetPlatform == "TMAPI" || targetPlatform == "HEN")
            {
                targetPlatform = "PS3";
            }

            if (targetPlatform != "PS4" && targetPlatform != "PS5" && targetPlatform != "PS3")
            {
                return (false, $"{console} injection routing is not fully implemented yet.");
            }

            string engineCode = "";
            switch (shortGameName.ToUpper())
            {
                case "GHOSTS": engineCode = "iw6"; break;
                case "AW": engineCode = "s1"; break;
                case "MWR": engineCode = "h1"; break;
                case "MW2CR": engineCode = "h2"; break;
                case "WW2": engineCode = "s2"; break;
                case "BO3": engineCode = "bo3"; break;
                case "MW3": engineCode = "iw5"; break;
                case "IW": engineCode = "iw7"; break;
                case "MW2019": engineCode = "iw8"; break;
                case "VANGUARD": engineCode = "s4"; break;
                case "MW22": engineCode = "iw9"; break;
                default: engineCode = shortGameName.ToLower(); break;
            }

            string vpsGameKey = (engineCode == "bo3" && targetPlatform != "PS3") ? "bo3_ps4" : $"{engineCode}{targetPlatform.ToLower()}";


            return await ProcessInjection(compiledBytes, vpsGameKey, mode.ToLower());
        }
    }
}


