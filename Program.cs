using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace p3dkg
{
    internal class Program
    {
        //Diffrent version of P3D required variant of registry keys (Uniqe License Key and some Progam-id Key)
        private sealed class Prepar3DRegistryKeys
        {
            public required List<string> ProgIdRegKey { get; set; }
            public required string LicenseRegKey { get; set; }

            public required CustemParameter Acdemic { get; set; }
            public required CustemParameter Professional { get; set; }
            public required CustemParameter ProfessionalPlus { get; set; }
        }

        private sealed class CustemParameter
        {
            public required string Edition { get; set; }
            public required string Option { get; set; }
        }

        private static readonly Dictionary<string, Prepar3DRegistryKeys> P3DVersions = new()
        {
            ["v3"] = new Prepar3DRegistryKeys
            {
                ProgIdRegKey = [
                    @"CLSID\{D1AEB7A1-2EFE-4911-9E61-A523DA588310}\ProgID",
                    @"CLSID\{40583312-6D7A-4667-888B-37CF9592140E}\ProgID",
                    @"CLSID\{EEFC873E-1515-4580-9078-87F06016A1BF}\ProgID",
                    @"CLSID\{3033FB27-911D-48F9-B7CF-4CE041EE313E}\ProgID",
                    @"CLSID\{F0E9E79B-83C0-4200-A6AD-CC7C8B13F07B}\ProgID",
                    @"CLSID\{045B19DA-708B-4C27-B934-DE959B35CD3A}\ProgID"
                ],
                LicenseRegKey = @"SOFTWARE\Lockheed Martin\Prepar3D v3",
                Acdemic = new CustemParameter { Edition = "5992", Option = "1086" },
                Professional = new CustemParameter { Edition = "5968", Option = "1085" },
                ProfessionalPlus = new CustemParameter { Edition = "5944", Option = "1072" }
            },
            ["v4"] = new Prepar3DRegistryKeys
            {
                ProgIdRegKey = [
                    @"CLSID\{1A495DD3-72C8-4CA3-BB4F-573B7522472F}\ProgID",
                    @"CLSID\{5A784295-54E7-4790-8B94-878BF1926D17}\ProgID",
                    @"CLSID\{7857C3BB-7880-48FA-BF98-068C55725EB3}\ProgID",
                    @"CLSID\{078CDE1E-B000-43B2-954D-136D4AF8DF5D}\ProgID",
                    @"CLSID\{7A5C6F9D-55FD-45C4-9B99-EFA5A6A2B583}\ProgID",
                    @"CLSID\{D95E5241-69AD-4841-AB7B-BF5CE97A790B}\ProgID"
                ],
                LicenseRegKey = @"SOFTWARE\Lockheed Martin\Prepar3D v4",
                Acdemic = new CustemParameter { Edition = "6016", Option = "1089" },
                Professional = new CustemParameter { Edition = "6040", Option = "1096" },
                ProfessionalPlus = new CustemParameter { Edition = "6064", Option = "1103" }
            },
            //v5 is the latest completed version
            ["v5"] = new Prepar3DRegistryKeys
            {
                ProgIdRegKey = [@"CLSID\{49750546-2367-4E1C-9ADD-ED46247A70AA}\ProgID"],
                LicenseRegKey = @"SOFTWARE\Lockheed Martin\Prepar3D v5",
                Acdemic = new CustemParameter { Edition = "6112", Option = "1089" },
                Professional = new CustemParameter { Edition = "6136", Option = "1096" },
                ProfessionalPlus = new CustemParameter { Edition = "6160", Option = "1103" }
            },
            //Not find out yet
            // just copy v5 
            ["v6"] = new Prepar3DRegistryKeys
            {
                ProgIdRegKey = [@"CLSID\{49750546-2367-4E1C-9ADD-ED46247A70AA}\ProgID"],
                LicenseRegKey = @"SOFTWARE\Lockheed Martin\Prepar3D v6",
                Acdemic = new CustemParameter { Edition = "6112", Option = "1089" },
                Professional = new CustemParameter { Edition = "6136", Option = "1096" },
                ProfessionalPlus = new CustemParameter { Edition = "6160", Option = "1103" }
            }
        };
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
            string manualVersion = "";
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                    case "-h":
                        showHelp = true;
                        break;
                    case "--version":
                    case "-v":
                        manualVersion = args[++i].ToLowerInvariant();
                        if (!P3DVersions.ContainsKey(manualVersion))
                        {
                            Console.WriteLine("Error: Unsupported version specified. Supported versions are: v3, v4, v5, v6.");
                            return;
                        }
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
                                Console.WriteLine($"[VERBOSE] Manual activation requested. Type: {type}, Ver: {manualVersion} Arg: {manualArg}");
                            if (P3DVersions.ContainsKey(manualVersion))
                            {
                                ManualActivate(manualArg, manualVersion);
                            }
                            else
                            {
                                ManualActivate(manualArg);
                            }
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Error: --type requires an argument.");
                            return;
                        }
                    case "--verbose":
                    case "-V":
                        verbose = true;
                        break;
                }
            }

            if (showHelp)
            {
                Console.Beep();
                Console.WriteLine("This program is designed to activate Prepar3D Software, maximum version is V5. You can either let the program automatically decide what type of license to use or manually specify it: 'academic', 'professional', 'professional-plus'.\r\n");
                Console.WriteLine("BTW, it is a open-source program, please give me a star if you success activate! https://github.com/jindongjie/p3dkg \r\n");
                Console.WriteLine("Usage:");
                Console.WriteLine("  --help   | -h    Show help");
                Console.WriteLine("  --type   | -t    Specify type (Default deside by program), 'acdemic' / 'professinal' / 'professinal-plus'");
                Console.WriteLine(" --version | -v    Specify version(Default deside by program), 'v3','v4','v5' 'v6'");
                Console.WriteLine(" --verbose | -V    Enable verbose output (optional)\r\n");
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
            bool isPrivileged = CheckIfElevated();
            string licenseType = "";

            if (verbose)
                Console.WriteLine("[VERBOSE] Checking registry for installed license type...");

            string version = AutoDesideVersion();

            using (RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey(P3DVersions[version].LicenseRegKey, false))
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
                    Console.WriteLine("It seems that you did not install any version of Prepar3D on this computer.");
                    return;
                }
            }
            if (isPrivileged)
            {
                Console.WriteLine("You are run as privileged user, you can choose activate all version, and you can switch between them");
            }
            else
            {
                Console.WriteLine("You can only activate the version you installed in the secure activator. You can't switch between versions. This program IS NOT VIRUS! it is open-source program! check out https://github.com/jindongjie/p3dkg");
                if (verbose)
                    Console.WriteLine("[VERBOSE] Not running as privileged user. Will activate installed license type only.");
            }
            switch (licenseType)
            {
                case "Academic":
                    Activate("academic", version, isPrivileged);
                    break;
                case "Professional":
                    Activate("professional", version, isPrivileged);
                    break;
                case "Professional Plus":
                    Activate("professional-plus", version, isPrivileged);
                    break;
                default:
                    Console.WriteLine("Unknown or missing license type in registry.");
                    return;
            }

        }

        public static void ManualActivate(string manualArg)
        {
            bool isPrivileged = CheckIfElevated();
            string version = AutoDesideVersion();
            if (verbose)
                Console.WriteLine($"[VERBOSE] Manual activation. License type: {manualArg}, AutoDeside versions: {version} Privileged: {isPrivileged}");
            Activate(manualArg, version, isPrivileged);
        }

        public static void ManualActivate(string manualArg, string manualVersion)
        {
            bool isPrivileged = CheckIfElevated();
            if (verbose)
                Console.WriteLine($"[VERBOSE] Manual activation. License type: {manualArg}, Software Verions: {manualVersion} Privileged: {isPrivileged}");
            Activate(manualArg, manualVersion, isPrivileged);
        }

        private static string AutoDesideVersion()
        {
            foreach (var version in P3DVersions)
            {
                if (Registry.LocalMachine.OpenSubKey(version.Value.LicenseRegKey, false) != null)
                {
                    if (verbose)
                        Console.WriteLine($"[VERBOSE] Found Prepar3D version: {version.Key}");
                    return version.Key;
                }
            }
            return ""; // No version found
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        //need Windows only API
        private static void Activate(string licenseArg, string ProgramVersion, bool isPrivileged)
        {
            try
            {
                //Check if the specified version is supported
                if (P3DVersions.ContainsKey(ProgramVersion))
                {
                    if (verbose)
                        Console.WriteLine($"[VERBOSE] Using Prepar3D version: {ProgramVersion}");
                }
                else
                {
                    Console.WriteLine("Unknown Prepar3D version specified.");
                    return;
                }

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
                InstallID = GetInstallationID(P3DVersions[ProgramVersion].LicenseRegKey);
                if (string.IsNullOrEmpty(InstallID))
                {
                    Console.WriteLine("This version of Prepar3D is incompatible with the activator. Please look for an update!");
                    return;
                }
                if (verbose)
                    Console.WriteLine($"[VERBOSE] Installation ID: {InstallID}");

                string license = "";
                var gen = new Gen();

                //Generate license!
                switch (licenseArg.ToLowerInvariant())
                {
                    case "academic":
                        if (verbose)
                            Console.WriteLine("[VERBOSE] Generating Academic license...");
                        license = gen.GetLicense(hashedVSN, hashedNICADDR, hashedCNAME, P3DVersions[ProgramVersion].Acdemic.Edition, P3DVersions[ProgramVersion].Acdemic.Option, InstallID, dtExpiration);
                        if (isPrivileged)
                        {
                            if (verbose)
                                Console.WriteLine("[VERBOSE] Setting registry license to 'Academic'.");
                            SetRegistryLicense("Academic", ProgramVersion);
                        }
                        break;
                    case "professional":
                        if (verbose)
                            Console.WriteLine("[VERBOSE] Generating Professional license...");
                        license = gen.GetLicense(hashedVSN, hashedNICADDR, hashedCNAME, P3DVersions[ProgramVersion].Professional.Edition, P3DVersions[ProgramVersion].Professional.Option, InstallID, dtExpiration);
                        if (isPrivileged)
                        {
                            if (verbose)
                                Console.WriteLine("[VERBOSE] Setting registry license to 'Professional'.");
                            SetRegistryLicense("Professional", ProgramVersion);
                        }
                        break;
                    case "professional-plus":
                        if (verbose)
                            Console.WriteLine("[VERBOSE] Generating Professional Plus license...");
                        license = gen.GetLicense(hashedVSN, hashedNICADDR, hashedCNAME, P3DVersions[ProgramVersion].ProfessionalPlus.Edition, P3DVersions[ProgramVersion].ProfessionalPlus.Option, InstallID, dtExpiration);
                        if (isPrivileged)
                        {
                            if (verbose)
                                Console.WriteLine("[VERBOSE] Setting registry license to 'Professional Plus'.");
                            SetRegistryLicense("Professional Plus", ProgramVersion);
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown license type specified.");
                        return;
                }

                //Directly write generated license to regist table
                if (isPrivileged)
                {
                    if (verbose)
                        Console.WriteLine("[VERBOSE] Writing activation information to registry...");
                    using (var registryKey = Registry.ClassesRoot.OpenSubKey(P3DVersions[ProgramVersion].LicenseRegKey, true))
                    {
                        registryKey?.SetValue("", license);
                    }
                    Console.WriteLine("New activation information has been written to registry.");
                }
                //Create a .reg file for manual import
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
                        File.WriteAllText("p3d_activation.reg", stringBuilder.ToString());
                        Console.WriteLine("A file 'p3d_activation.reg' has been created. Please double-click it to import the license into your computer system.");
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

        private static void SetRegistryLicense(string license, string programVersion)
        {
            if (verbose)
                Console.WriteLine($"[VERBOSE] Writing license type '{license}' to registry key '{P3DVersions[programVersion].LicenseRegKey}'...");
            using var reg = Registry.LocalMachine.OpenSubKey(P3DVersions[programVersion].LicenseRegKey, true);
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
            string hash = Convert.ToBase64String(SHA512.HashData(Encoding.ASCII.GetBytes(input)));
            if (verbose)
                Console.WriteLine($"[VERBOSE] Hashed '{input}' to '{hash}'");
            return hash;
        }

        private static string GetInstallationID(string licregkey)
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
            string[] asciiArt =
            [
                @"",
                @"__/\\\\\\\\\\\\\_______/\\\\\\\\\\___/\\\\\\\\\\\\_____/\\\________/\\\_____/\\\\\\\\\\\\_        ",
                @" _\/\\\/////////\\\___/\\\///////\\\_\/\\\////////\\\__\/\\\_____/\\\//____/\\\//////////__       ",
                @"  _\/\\\_______\/\\\__\///______/\\\__\/\\\______\//\\\_\/\\\__/\\\//______/\\\_____________      ",
                @"   _\/\\\\\\\\\\\\\/__________/\\\//___\/\\\_______\/\\\_\/\\\\\\//\\\_____\/\\\____/\\\\\\\_     ",
                @"    _\/\\\/////////___________\////\\\__\/\\\_______\/\\\_\/\\\//_\//\\\____\/\\\___\/////\\\_    ",
                @"     _\/\\\_______________________\//\\\_\/\\\_______\/\\\_\/\\\____\//\\\___\/\\\_______\/\\\_   ",
                @"      _\/\\\______________/\\\______/\\\__\/\\\_______/\\\__\/\\\_____\//\\\__\/\\\_______\/\\\_  ",
                @"       _\/\\\_____________\///\\\\\\\\\/___\/\\\\\\\\\\\\/___\/\\\______\//\\\_\//\\\\\\\\\\\\/__ ",
                @"        _\///________________\/////////_____\////////////_____\///________\///___\////////////____",
                @"",
                @" Prepar3D Key Generator                 "
            ];
            foreach (var line in asciiArt)
                Console.WriteLine(line);
        }
    }
}
