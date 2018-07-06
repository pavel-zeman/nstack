using System;
using System.Diagnostics;
using Microsoft.Mse.Library;
using Microsoft.Samples.Debugging.CorDebug;

[assembly: CLSCompliant(false)]

namespace nstack
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            int pid;
            if (args.Length == 1 && int.TryParse(args[0], out pid))
            {
                try
                {
                    var process = Process.GetProcessById(int.Parse(args[0]));
                    
                    var mh = new CLRMetaHost();
                    mh.EnumerateLoadedRuntimes(process.Id);
                    var version = MdbgVersionPolicy.GetDefaultAttachVersion(process.Id);
                    var debugger = new CorDebugger(version);
                    var processInfo = new ProcessInfo(process, debugger);
                    foreach (var line in processInfo.GetDisplayStackTrace())
                    {
                        Console.WriteLine(line);
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error getting process thread dump: " + e);
                }
            }
            else
            {
                Console.Error.WriteLine("Simple utility to generate .NET managed process thread dump (similar to Java jstack)");
                Console.Error.WriteLine("Usage: " + AppDomain.CurrentDomain.FriendlyName + " <PID>");
            }
        }
    }
}