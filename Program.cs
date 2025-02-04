using System.Diagnostics;
using CommandLine;
using DotNetEnv;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MSPChallenge_Client_Launcher;

internal static class Program
{
    private class Options
    {
        [Option("msp-client-folder-path", Required = false, HelpText = "Specifies the path to the MSP-Challenge client folder. It should contain the MSP-Challenge.exe.")]
        public string? MspClientFolderPath { get; set; }

        [Option("num-clients", Required = false, Default = 1, HelpText = "Specifies the number of clients to launch. Default is 1. **Number is limited by the system memory.** see --memory-limit-percentage and --memory-penalty-per-client-percentage.")]
        public int NumClients { get; set; }
        
        [Option("client-group-size", Required = false, Default = 5, HelpText = "Specifies the number of clients in each group. Default is 5.")]
        public int ClientGroupSize { get; set; }        
        
        [Option("delay-between-clients-sec", Required = false, Default = 4, HelpText = "Specifies the delay between clients in seconds. Default is 4.")]
        public int DelayBetweenClientsSec { get; set; }        
        
        [Option("delay-between-client-groups-sec", Required = false, Default = 20, HelpText = "Specifies the delay between client groups in seconds. Default is 20.")]
        public int DelayBetweenClientGroupsSec { get; set; }
        
        [Option("memory-limit-percentage", Required = false, Default = 80, HelpText = "Specifies the memory limit as a percentage of total system memory. Default is 80%.")]
        public int MemoryLimitPercentage { get; set; }
        
        [Option("memory-penalty-per-client-percentage", Required = false, Default = 0.4, HelpText = "Specifies the memory penalty per client as a percentage of total system memory. Default is 0.4%.")]
        public double MemoryPenaltyPerClientPercentage { get; set; }
        
        [Option("kill-all-client-processes-at-start", Required = false, Default = false, HelpText = "Specifies whether to kill all the client processes at the start. Default is false.")]
        public bool KillAllClientProcessesAtStart { get; set; }        
    }

    private static void Main(string[] args)
    {
        Console.WriteLine("MSP-Challenge Client Launcher");
        Console.WriteLine("-------------------------------");
        Console.WriteLine("This program can launch multiple MSP-Challenge clients keeping the memory usage under a certain limit.");
        Console.WriteLine("It will pass any unknown arguments to the MSP-Challenge client, so you can pass client command line arguments, e.g.:");
        Console.WriteLine("  MSPChallenge_Client_Launcher --num-clients=20 Team=Admin User=marin Password=test ServerAddress=http://localhost ConfigFileName=North_Sea_basic AutoLogin=1");
        Console.WriteLine("-------------------------------");
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(opts => RunOptions(opts, args))
            .WithNotParsed(HandleParseError);
    }

    private static void RunOptions(Options opts, string[] args)
    {
        if (opts.KillAllClientProcessesAtStart)
        {
            var processes = Process.GetProcessesByName("MSP-Challenge");
            foreach (var process in processes)
            {
                process.Kill();
                Console.WriteLine($"Killed process with ID {process.Id}");
            }
        }
        
        Env.Load(".env.local");
        var mspClientFolderPath = opts.MspClientFolderPath ?? Environment.GetEnvironmentVariable("MSP_CLIENT_FOLDER_PATH");
        while (mspClientFolderPath == null || !IsValidFolderPath(mspClientFolderPath))
        {
            Console.Write("Please enter the path to the MSP-Challenge folder: ");
            mspClientFolderPath = Console.ReadLine();
        }
        UpdateEnvLocalFile("MSP_CLIENT_FOLDER_PATH", mspClientFolderPath);
        Console.WriteLine("MSP_CLIENT_FOLDER_PATH: " + mspClientFolderPath);

        var numClients = opts.NumClients;

        // Get the properties of the Options class
        var optionProperties = typeof(Options).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                              .Select(p => p.Name.ToLower().Replace('_', '-'));

        // Filter out the arguments that match the properties of the Options class
        var filteredArgs = args.Where(arg => !optionProperties.Any(opt => arg.StartsWith($"--{opt}"))).ToArray();

        var processIds = new List<int>();
        for (var i = 0; i < numClients; i++)
        {
            var estimatedMemoryUsage = GetSystemMemoryUsagePercentage() + (i+1)*opts.MemoryPenaltyPerClientPercentage;
            Console.WriteLine($"Estimated memory usage: {estimatedMemoryUsage:F2}%");
            if (estimatedMemoryUsage > opts.MemoryLimitPercentage)
            {
                Console.WriteLine("Memory usage exceeded limit. Stopping the launch of new clients. Current number of clients: " + i);
                break;
            }            
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "MSP-Challenge.exe",
                    Arguments = string.Join(' ', filteredArgs),
                    WorkingDirectory = mspClientFolderPath,
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            processIds.Add(process.Id);
            Console.WriteLine($"# {(i+1)}. Started process with ID {process.Id}. Current memory usage: {GetSystemMemoryUsagePercentage():F2}%");
            Thread.Sleep(opts.DelayBetweenClientsSec * 1000);
            // If the number of clients is a multiple of the group size, wait for the delay between groups
            if ((i + 1) % opts.ClientGroupSize == 0 && (i + 1) < numClients)
            {
                Thread.Sleep(opts.DelayBetweenClientGroupsSec * 1000);
            }
        }
        
        // Start a thread to update the console text with current memory usage
        var updateThread = new Thread(() =>
        {
            while (true)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Press any key to kill all the clients... Or press Ctrl+C to exit. Current memory usage: {GetSystemMemoryUsagePercentage():F2}%");
                Thread.Sleep(1000);
            }
        });
        updateThread.IsBackground = true;
        updateThread.Start();        
        
        // prompt the user to kill all the clients
        Console.ReadKey();
        foreach (var pid in processIds)
        {
            try
            {
                Process.GetProcessById(pid).Kill();
                Console.WriteLine($"Killed process with ID {pid}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to kill process with ID {pid}: {ex.Message}");
            }
        }        
    }
    
    private static float GetSystemMemoryUsagePercentage()
    {
        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(memStatus))
        {
            return (float)memStatus.dwMemoryLoad;
        }
        throw new InvalidOperationException("Unable to get memory status.");
    }   

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);    
    
    private static void HandleParseError(IEnumerable<Error> errs)
    {
        // Handle errors (if any)
    }

    private static bool IsValidFolderPath(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return false;
        }

        if (!Directory.Exists(folderPath))
        {
            return false;
        }

        var mspChallengeExePath = Path.Combine(folderPath, "MSP-Challenge.exe");
        return File.Exists(mspChallengeExePath);
    }

    private static void UpdateEnvLocalFile(string variable, string newValue)
    {
        var lines = File.ReadAllLines(".env.local");
        var updated = false;

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith($"{variable}=")) continue;
            lines[i] = $"{variable}={newValue}";
            updated = true;
            break;
        }

        if (!updated)
        {
            // If the variable was not found, add it to the end of the file
            Array.Resize(ref lines, lines.Length + 1);
            lines[^1] = $"{variable}={newValue}";
        }

        File.WriteAllLines(".env.local", lines);
    }
}