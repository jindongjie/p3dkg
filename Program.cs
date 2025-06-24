using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace p3dv5kg
{
    internal class Program
    {
        private static readonly List<string> regkeys =
        [
            "CLSID\\{49750546-2367-4E1C-9ADD-ED46247A70AA}\\ProgID"
        ];
        private static readonly string licregkey = "SOFTWARE\\Lockheed Martin\\Prepar3D v5";
        private static string VSN = ""; //Volume Serial Number
        private static string CNAME = ""; //Computer Name
        private static string NICADDR = ""; //Network Interface Card Address
        private static string hashedVSN = ""; //Hashed Volume Serial Number
        private static string hashedCNAME = ""; //Hashed Computer Name
        private static string hashedNICADDR = ""; //Hashed Network Interface Card Address
        private static string InstallID = "";
        private static readonly DateTime dtExpiration = DateTime.Now.AddYears(10);
        private static bool verbose = false;

        static void Main(string[] args)
        {
            PrintAsciiArtTitle();
            if (!OperatingSystem.IsWindows()) // Only Windows is supported
            {
                if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine("This program is designed for Windows. On Linux, you need running it using Wine.");
                }
                else
                {
                    Console.WriteLine("This program is designed for Windows. For your platform, I recommend using the X-Plane series flight simulator.");
                }
                return;
            }

            bool showHelp = false;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                    case "-h":
                        showHelp = true;
                        break;
                    case "--type":
                    case "-t":
                        if (i + 1 < args.Length)
                        {
                            string type = args[++i].ToLowerInvariant();
                            if (type != "acdemic" && type != "professinal" && type != "professinal-plus")
                            {
                                Console.WriteLine("Error: use ‘-h’ to check out type usage.");
                                return;
                            }
                            string manualArg = type;
                            if (verbose)
                                Console.WriteLine($"[VERBOSE] Manual activation requested. Type: {type}, Arg: {manualArg}");
                            ManualActivate(manualArg);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Error: --type requires an argument.");
                            return;
                        }
                    case "--verbose":
                    case "-v":
                        verbose = true;
                        break;
                }
            }

            if (showHelp)
            {
                Console.Beep();
                Console.WriteLine("This program is designed to activate Prepar3D Software, maximum version is V5. You can either let the program automatically decide what type of license to use or manually specify it: 'academic', 'professional', 'professional-plus'.\r\n");
                Console.WriteLine("Usage:");
                Console.WriteLine("  --help | -h    Show help");
                Console.WriteLine("  --type | -t    Specify type (Default deside by program), 'acdemic' / 'professinal' / 'professinal-plus'");
                Console.WriteLine("  --verbose | -v Enable verbose output (optional)\r\n");
                return;
            }
            else
            {
                if (verbose)
                {
                    Console.WriteLine("[VERBOSE] Verbose mode enabled.");
                    Console.WriteLine("[VERBOSE] Starting auto activation process.");
                }
                AutoActivate();
            }
        }

        public static void AutoActivate()
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("This function is only supported on Windows.");
                return;
            }

            bool isPrivileged = CheckIfElevated();
            string licenseType = "";

            if (verbose)
                Console.WriteLine("[VERBOSE] Checking registry for installed license type...");

            using (RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey(licregkey, false))
            {
                if (registryKey != null)
                {
                    object? licenseValue = registryKey.GetValue("License");
                    licenseType = licenseValue?.ToString() ?? "";
                    if (verbose)
                        Console.WriteLine($"[VERBOSE] Found license type in registry: '{licenseType}'");
                }
                else
                {
                    Console.WriteLine("It seems that you did not install Prepar3D v5 yet.");
                    return;
                }
            }

            if (!isPrivileged)
            {
                Console.WriteLine("You can only activate the version you installed in the secure activator. You can't switch between versions.");
                if (verbose)
                    Console.WriteLine("[VERBOSE] Not running as privileged user. Will activate installed license type only.");
                switch (licenseType)
                {
                    case "Academic":
                        Activate("academic", isPrivileged);
                        break;
                    case "Professional":
                        Activate("professional", isPrivileged);
                        break;
                    case "Professional Plus":
                        Activate("professional-plus", isPrivileged);
                        break;
                    default:
                        Console.WriteLine("Unknown or missing license type in registry.");
                        return;
                }
            }
            else
            {
                Console.WriteLine("You can activate any license no matter which version you installed. You can also switch between versions just by re-activating.");
                if (verbose)
                    Console.WriteLine("[VERBOSE] Running as privileged user. Defaulting to 'academic' license activation.");
                // Default to Academic if not specified
                Activate("academic", isPrivileged);
            }
        }

        public static void ManualActivate(string manualArg)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("This function is only supported on Windows.");
                return;
            }
            bool isPrivileged = CheckIfElevated();
            if (verbose)
                Console.WriteLine($"[VERBOSE] Manual activation. License type: {manualArg}, Privileged: {isPrivileged}");
            Activate(manualArg, isPrivileged);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        //need Windows only API
        private static void Activate(string licenseArg, bool isPrivileged)
        {
            try
            {
                //Gather system information
                if (verbose)
                    Console.WriteLine("[VERBOSE] Gathering system information for activation...");

                VSN = GetRandomVolumeSerial().ToString();
                if (verbose)
                    Console.WriteLine($"[VERBOSE] Volume Serial Number: {VSN}");

                CNAME = Environment.MachineName;
                if (verbose)
                    Console.WriteLine($"[VERBOSE] Computer Name: {CNAME}");

                var nics = NetworkInterface.GetAllNetworkInterfaces();
                if (nics.Length == 0)
                    throw new Exception("No network interfaces found.");
                NICADDR = nics[0].GetPhysicalAddress().ToString();
                if (verbose)
                    Console.WriteLine($"[VERBOSE] NIC Address: {NICADDR}");

                //Hash the values
                hashedVSN = GetHashedBase64String(VSN);
                hashedCNAME = GetHashedBase64String(CNAME);
                hashedNICADDR = GetHashedBase64String(NICADDR);

                if (verbose)
                {
                    Console.WriteLine($"[VERBOSE] Hashed VSN: {hashedVSN}");
                    Console.WriteLine($"[VERBOSE] Hashed CNAME: {hashedCNAME}");
                    Console.WriteLine($"[VERBOSE] Hashed NICADDR: {hashedNICADDR}");
                }
                //Installation ID validation
                InstallID = GetInstallationID();
                if (string.IsNullOrEmpty(InstallID))
                {
                    Console.WriteLine("This version of Prepar3D is incompatible with the activator. Please look for an update!");
                    return;
                }
                if (verbose)
                    Console.WriteLine($"[VERBOSE] Installation ID: {InstallID}");

                string license = "";
                var gen = new Gen();
                switch (licenseArg.ToLowerInvariant())
                {
                    case "academic":
                        if (verbose)
                            Console.WriteLine("[VERBOSE] Generating Academic license...");
                        license = gen.GetLicense(hashedVSN, hashedNICADDR, hashedCNAME, "6112", "1089", InstallID, dtExpiration);
                        if (isPrivileged)
                        {
                            if (verbose)
                                Console.WriteLine("[VERBOSE] Setting registry license to 'Academic'.");
                            SetRegistryLicense("Academic");
                        }
                        break;
                    case "professional":
                        if (verbose)
                            Console.WriteLine("[VERBOSE] Generating Professional license...");
                        license = gen.GetLicense(hashedVSN, hashedNICADDR, hashedCNAME, "6136", "1096", InstallID, dtExpiration);
                        if (isPrivileged)
                        {
                            if (verbose)
                                Console.WriteLine("[VERBOSE] Setting registry license to 'Professional'.");
                            SetRegistryLicense("Professional");
                        }
                        break;
                    case "professional-plus":
                        if (verbose)
                            Console.WriteLine("[VERBOSE] Generating Professional Plus license...");
                        license = gen.GetLicense(hashedVSN, hashedNICADDR, hashedCNAME, "6160", "1103", InstallID, dtExpiration);
                        if (isPrivileged)
                        {
                            if (verbose)
                                Console.WriteLine("[VERBOSE] Setting registry license to 'Professional Plus'.");
                            SetRegistryLicense("Professional Plus");
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown license type specified.");
                        return;
                }
                //Run on Admin mode
                if (isPrivileged)
                {
                    if (verbose)
                        Console.WriteLine("[VERBOSE] Writing activation information to registry...");
                    using (var registryKey = Registry.ClassesRoot.OpenSubKey(regkeys[0], true))
                    {
                        registryKey?.SetValue("", license);
                    }
                    Console.WriteLine("New activation information has been written to registry.");
                }
                else
                {
                    try
                    {
                        if (verbose)
                            Console.WriteLine("[VERBOSE] Creating .reg file for manual import...");
                        var stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine("Windows Registry Editor Version 5.00");
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine("[HKEY_CLASSES_ROOT\\CLSID\\{49750546-2367-4E1C-9ADD-ED46247A70AA}\\ProgID]");
                        stringBuilder.AppendLine("@=\"" + license.Replace("\"", "\\\"") + "\"");
                        File.WriteAllText("p3dactivation.reg", stringBuilder.ToString());
                        Console.WriteLine("A file p3dactivation.reg has been created. Please double-click it to import the license into your computer system.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not create your license info file\n" + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed gathering system information: " + ex.Message);
            }
        }

        private static void SetRegistryLicense(string license)
        {
            if (verbose)
                Console.WriteLine($"[VERBOSE] Writing license type '{license}' to registry key '{licregkey}'...");
            using var reg = Registry.LocalMachine.OpenSubKey(licregkey, true);
            reg?.SetValue("License", license);
        }

        private static bool CheckIfElevated()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            bool elevated = current.Owner != current.User;
            if (verbose)
                Console.WriteLine($"[VERBOSE] CheckIfElevated: {elevated}");
            return elevated;
        }

        private static uint GetRandomVolumeSerial()
        {
            var buffer = new byte[4];
            new Random().NextBytes(buffer);
            uint serial = BitConverter.ToUInt32(buffer, 0);
            if (verbose)
                Console.WriteLine($"[VERBOSE] Generated random volume serial: {serial}");
            return serial == 0 ? 1u : serial;
        }

        private static string GetHashedBase64String(string input)
        {
            using (var sha = SHA512.Create())
            {
                string hash = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(input)));
                if (verbose)
                    Console.WriteLine($"[VERBOSE] Hashed '{input}' to '{hash}'");
                return hash;
            }
        }

        private static string GetInstallationID()
        {
            string? setupPath = "";
            if (verbose)
                Console.WriteLine($"[VERBOSE] Reading installation path from registry key '{licregkey}'...");
            using (var reg = Registry.LocalMachine.OpenSubKey(licregkey, false))
            {
                if (reg != null)
                {
                    object val = reg.GetValue("SetupPath", "");
                    setupPath = val != null ? val.ToString() : "";
                    if (verbose)
                        Console.WriteLine($"[VERBOSE] SetupPath: {setupPath}");
                }
            }
            if (!string.IsNullOrEmpty(setupPath))
            {
                string apiPath = Path.Combine(setupPath, "api.dll");
                if (verbose)
                    Console.WriteLine($"[VERBOSE] Looking for api.dll at: {apiPath}");
                if (File.Exists(apiPath))
                {
                    byte[] bytes = File.ReadAllBytes(apiPath);
                    int len = bytes.Length;
                    for (int i = 0; i < len - 24; ++i)
                    {
                        if (bytes[i] == 45 && bytes[i + 6] == 45 && bytes[i + 12] == 45 && bytes[i + 18] == 45 && bytes[i + 24] == 45)
                        {
                            int index1 = i - 5;
                            if (index1 >= 0 && index1 + 31 <= len)
                            {
                                string id = Encoding.ASCII.GetString(bytes, index1, 31);
                                if (verbose)
                                    Console.WriteLine($"[VERBOSE] Found Installation ID: {id}");
                                return id;
                            }
                            break;
                        }
                    }
                    if (verbose)
                        Console.WriteLine("[VERBOSE] Installation ID pattern not found in api.dll.");
                }
                else
                {
                    Console.WriteLine("Your Installation seems to be damaged. A required file was not found. I can't continue");
                    return "";
                }
            }
            else
            {
                Console.WriteLine("Your Installation seems to be damaged. Registry entries are missing or damaged. I can't continue");
            }
            return "";
        }
        private static void PrintAsciiArtTitle()
        {
            string[] asciiArt = new[]
            {
                @"",
                @" ________  ________  ________  ___      ___ ________  ___  __    ________     ",
                @"|\   __  \|\_____  \|\   ___ \|\  \    /  /|\   ____\|\  \|\  \ |\   ____\    ",
                @"\ \  \|\  \|____|\ /\ \  \_|\ \ \  \  /  / | \  \___|\ \  \/  /|\ \  \___|    ",
                @" \ \   ____\    \|\  \ \  \ \\ \ \  \/  / / \ \_____  \ \   ___  \ \  \  ___  ",
                @"  \ \  \___|   __\_\  \ \  \_\\ \ \    / /   \|____|\  \ \  \\ \  \ \  \|\  \ ",
                @"   \ \__\     |\_______\ \_______\ \__/ /      ____\_\  \ \__\\ \__\ \_______\",
                @"    \|__|     \|_______|\|_______|\|__|/      |\_________\|__| \|__|\|_______|",
                @"                                              \|_________|                    ",
                @"          ",
                @" Prepar3D v5 Key Generator                 "
            };
            foreach (var line in asciiArt)
                Console.WriteLine(line);
        }
    }
}
