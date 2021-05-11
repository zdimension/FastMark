using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using CSharpLib;
using Microsoft.VisualBasic.Devices;
using Shortcut = CSharpLib.Shortcut;

namespace FastMark
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length == 0)
            {
                var ipath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FastMark");
                var sendto = Environment.GetFolderPath(Environment.SpecialFolder.SendTo);
                var exename = Path.Combine(ipath, "FastMark.exe");
                var shortcut = Path.Combine(sendto, "FastMark.lnk");

                if (Directory.Exists(ipath))
                {
                    Directory.Delete(ipath, true);
                    File.Delete(shortcut);
                    TaskDialog.ShowDialog(new TaskDialogPage
                    {
                        Caption = "FastMark",
                        Text = "Uninstalled successfully!",
                        Buttons = new TaskDialogButtonCollection { TaskDialogButton.OK },
                        Icon = TaskDialogIcon.Information
                    });
                }
                else
                {
                    Directory.CreateDirectory(ipath);
                    var program = Process.GetCurrentProcess().MainModule!.FileName!;
                    var parentDir = Directory.GetParent(program!);
                    new Computer().FileSystem.CopyDirectory(parentDir!.FullName, ipath);
                    new Shortcut().CreateShortcutToFile(exename, shortcut, IconLocation: @"C:\windows\system32\shell32.dll,156");
                    TaskDialog.ShowDialog(new TaskDialogPage
                    {
                        Caption = "FastMark",
                        Text = "Installed successfully!",
                        Buttons = new TaskDialogButtonCollection { TaskDialogButton.OK },
                        Icon = TaskDialogIcon.Information
                    });
                }
            }
            else
            {
                Application.Run(new MainWindow());
            }
        }
    }
}