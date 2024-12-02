using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public extern static bool GetTokenInformation(IntPtr hToken, int tokenInfoClass, IntPtr TokenInformation, int tokeInfoLength, out int reqLength);

    public const int TokenElevation = 20;
    public const uint TOKEN_QUERY = 0x0008;

    static void Main(string[] args)
    {
        var steamProcess = Process.GetProcessesByName("steam").FirstOrDefault();
        if (steamProcess != null)
        {
            bool isElevated = IsProcessElevated(steamProcess);
            CloseSteam();
            WaitForSteamToClose();

            if (isElevated)
            {
                StartSteam(false);
            }
            else
            {
                StartSteam(true);
            }
        }
        else
        {
            Console.WriteLine("Steam is not running. Starting Steam with elevation.");
            StartSteam(true);
        }
    }

    static bool IsProcessElevated(Process process)
    {
        try
        {
            IntPtr tokenHandle;
            if (!OpenProcessToken(process.Handle, TOKEN_QUERY, out tokenHandle))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                int elevationResult = 0;
                int elevationResultSize = Marshal.SizeOf((int)elevationResult);
                IntPtr elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);

                try
                {
                    if (GetTokenInformation(tokenHandle, TokenElevation, elevationTypePtr, elevationResultSize, out elevationResultSize))
                    {
                        elevationResult = Marshal.ReadInt32(elevationTypePtr);
                        return elevationResult != 0;
                    }
                    else
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(elevationTypePtr);
                }
            }
            finally
            {
                CloseHandle(tokenHandle);
            }
        }
        catch (Win32Exception ex)
        {
            if (ex.Message.Contains("Access is denied"))
            {
                return true;
            }
            else
            {
                throw;
            }
        }
    }

    static void CloseSteam()
    {
        Process.Start(new ProcessStartInfo("cmd", $"/c start steam://exit") { CreateNoWindow = true });
    }

    static void WaitForSteamToClose()
    {
        while (Process.GetProcessesByName("steam").Any())
        {
            Thread.Sleep(1000);
        }
    }

    static void StartSteam(bool elevated)
    {
        ProcessStartInfo startInfo;
        if (elevated)
        {
            startInfo = new ProcessStartInfo("C:\\Program Files (x86)\\Steam\\Steam.exe") { Verb = "runas", CreateNoWindow = true, UseShellExecute = true };
        }
        else
        {
            startInfo = new ProcessStartInfo("C:\\Program Files (x86)\\Steam\\Steam.exe");
        }
        Process.Start(startInfo);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);
}