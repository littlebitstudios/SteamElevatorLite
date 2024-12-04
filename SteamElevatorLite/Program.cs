using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

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
        if (args.Length > 0 && args[0] == "trigger")
        {
            if (args.Length > 1 && args[1] == "--noconfirm")
            {
                Trigger(false);
            }
            else
            {
                Trigger();
            }
        }
        else if (IsAlreadyRunning())
        {
            var result = MessageBox.Show("Another instance of SteamElevatorLite is already running. Do you want to quit?", "Instance Running", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                HttpClient client = new HttpClient();
                client.GetAsync("http://localhost:12345/quit").Wait();
                Environment.Exit(0);
            }
            else
            {
                Environment.Exit(0);
            }
        }
        else
        {
            MessageBox.Show("SteamElevatorLite is running. If you want to quit, run the executable again or type \"http://localhost:12345/quit\" in your browser.", "SteamElevatorLite", MessageBoxButtons.OK, MessageBoxIcon.Information);
            StartHTTP();
        }
    }

    static void StartHTTP()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:12345/");
        listener.Start();

        Console.WriteLine("Listening on http://localhost:12345/");
        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.Url.AbsolutePath == "/trigger")
            {
                response.StatusCode = 200;
                response.StatusDescription = "OK";
                bool showConfirmation = true;
                if (request.QueryString["showConfirmation"] != null)
                {
                    bool.TryParse(request.QueryString["showConfirmation"], out showConfirmation);
                }
                response.Close();
                Trigger(showConfirmation);
            }
            else if (request.Url.AbsolutePath == "/quit")
            {
                response.StatusCode = 200;
                response.StatusDescription = "OK";
                response.Close();
                MessageBox.Show("SteamElevatorLite is quitting.", "SteamElevatorLite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(0);
            }
            else
            {
                response.StatusCode = 404;
                response.StatusDescription = "Not Found";
                response.Close();
            }
        }
    }

    static void Trigger(bool ShowConfirmation = true)
    {
        var steamProcess = Process.GetProcessesByName("steam").FirstOrDefault();
        if (steamProcess != null)
        {
            bool isElevated = IsProcessElevated(steamProcess);
            
            DialogResult confirm = DialogResult.No;

            if (ShowConfirmation)
            {
                confirm = MessageBox.Show($"Steam is running {(isElevated ? "with" : "without")} elevation. Do you want to restart Steam?", "Restart Steam", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }

            if (confirm == DialogResult.Yes || !ShowConfirmation)
            {
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
        }
        else
        {
            Console.WriteLine("Steam is not running. Starting Steam with elevation.");
            StartSteam(true);
        }
    }

    static bool IsAlreadyRunning()
    {
        var processes = Process.GetProcessesByName("SteamElevatorLite");
        return processes.Length > 1;
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