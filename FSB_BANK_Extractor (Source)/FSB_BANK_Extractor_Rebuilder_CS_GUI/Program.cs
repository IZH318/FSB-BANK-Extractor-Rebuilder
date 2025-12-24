/**
 * @file Program.cs
 * @brief The main entry point for the application.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This file configures the application environment and launches the main GUI form.
 * It includes High DPI awareness settings to ensure proper rendering on high-resolution displays.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-18
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices; // Required for DllImport to set DPI awareness.
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    internal static class Program
    {
        // Import the user32.dll function to set the process as DPI aware.
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Enable High DPI awareness for Windows Vista and newer (NT 6.0+).
            if (Environment.OSVersion.Version.Major >= 6)
            {
                SetProcessDPIAware();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FSB_BANK_Extractor_Rebuilder_CS_GUI());
        }
    }
}