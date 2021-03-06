﻿// Helpers...
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using TwainDirect.Support;
[assembly: CLSCompliant(true)]

namespace TwainDirect.Scanner
{
    /// <summary>
    /// Our entry point.  From here we'll dispatch to the mode: window, terminal
    /// or service, along with any interesting arguments...
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="a_aszArgs">interesting arguments</param>
        [STAThread]
        static void Main(string[] a_aszArgs)
        {
            string szExecutableName;
            string szWriteFolder;
            float fScale;
            Form1 form1;

            // Are we already running?
            Process[] aprocess = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location));
            foreach (Process process in aprocess)
            {
                // If it ain't us, it's somebody else...
                if (process.Id != Process.GetCurrentProcess().Id)
                {
                    MessageBox.Show("This program is already running.  If you don't see it on the screen, check the system tray for the TWAIN Direct icon, and right click on it for the list of options.", "TWAIN Direct on TWAIN Bridge");
                    Environment.Exit(1);
                }
            }

            // Load our configuration information and our arguments,
            // so that we can access them from anywhere in the code...
            if (!Config.Load(Application.ExecutablePath, a_aszArgs, "appdata.txt"))
            {
                MessageBox.Show("Error starting.  Try uninstalling and reinstalling this software.", "Error");
                Environment.Exit(1);
            }

            // Set up our data folders...
            szWriteFolder = Config.Get("writeFolder", "");
            szExecutableName = Config.Get("executableName", "");

            // Turn on logging...
            Log.Open(szExecutableName, szWriteFolder, 1);
            Log.SetLevel((int)Config.Get("logLevel", 0));
            Log.Info(szExecutableName + " Log Started...");

            // Make sure that any stale TwainDirectOnTwain processes are gone...
            foreach (Process processTwainDirectOnTwain in Process.GetProcessesByName("TwainDirect.OnTwain"))
            {
                try
                {
                    processTwainDirectOnTwain.Kill();
                }
                catch (Exception exception)
                {
                    Log.Error("unable to kill TwainDirect.OnTwain - " + exception.Message);
                }
            }

            // Figure out what we're doing...
            string szCommand;
            Mode mode = SelectMode(out szCommand, out fScale);

            // Pick our command...
            switch (mode)
            {
                // Uh-oh...
                default:
                    Log.Error("Unrecognized mode: " + mode);
                    break;

                case Mode.SERVICE:
                    //Service service = new Service();
                    //ServiceBase.Run(service);
                    break;

                case Mode.TERMINAL:
                    if (TwainLocalScanner.GetPlatform() == TwainLocalScanner.Platform.WINDOWS)
                    {
                        Interpreter.CreateConsole();
                    }
                    Terminal terminal = new TwainDirect.Scanner.Terminal();
                    switch (szCommand.ToLower())
                    {
                        default:
                            Log.Error("Unrecognized command: " + szCommand);
                            break;
                        case "register":
                            terminal.Register();
                            break;
                        case "start":
                            terminal.Start();
                            break;
                    }
                    terminal.Dispose();
                    break;

                // Fire up our application window...
                case Mode.WINDOW:
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    form1 = new Form1();
                    Application.Run(form1);
                    form1.Dispose();
                    break;
            }


            // All done...
            Log.Info(szExecutableName + " Log Ended...");
            Log.Close();
            Environment.Exit(0);
        }

        /// <summary>
        /// Select the mode for this session window, terminal or service based on the arguments.
        /// </summary>
        /// <param name="a_szCommand">command to perform</param>
        /// <param name="a_fScale">scale a window, if we have one</param>
        /// <returns>mode to use</returns>
        private static Mode SelectMode
        (
            out string a_szCommand,
            out float a_fScale
        )
        {
            Mode mode;

            // Init stuff...
            mode = Mode.WINDOW;
            a_szCommand = null;
            a_fScale = 1;

            // Sleep so we can attach and debug stuff...
            int iDelay = (int)Config.Get("delayTwainDirect.Scanner", 0);
            if (iDelay > 0)
            {
                Thread.Sleep(iDelay);
            }

            // Check for a command...
            a_szCommand = Config.Get("command", null);
            if (!string.IsNullOrEmpty(a_szCommand))
            {
                a_szCommand = a_szCommand.ToLower();
            }

            // Check for a mode...
            string szMode = Config.Get("mode", null);
            if (!string.IsNullOrEmpty(szMode))
            {
                switch (szMode.ToLower())
                {
                    default:
                        mode = Mode.UNKNOWN;
                        break;
                    case "terminal":
                        mode = Mode.TERMINAL;
                        break;
                    case "window":
                        mode = Mode.WINDOW;
                        break;
                    case "service":
                        mode = Mode.SERVICE;
                        break;
                }
            }

            // More arguments...
            a_fScale = (float)Config.Get("scale", 1.0);

            // Otherwise let the user interact with us...
            Log.Info("Mode: " + mode + " " + "command=" + ((a_szCommand == null) ? "*none*" : a_szCommand));
            return (mode);
        }

        /// <summary>
        /// The mode we'll run in...
        /// </summary>
        private enum Mode
        {
            UNKNOWN,
            SERVICE,
            TERMINAL,
            WINDOW
        }
    }
}
