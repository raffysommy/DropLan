using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace ShellContex
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length == 1)
            {
                Process[] pname = Process.GetProcessesByName("ProgettoApp");
                if (pname.Length == 0)
                {
                    String current = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                    String path = Path.GetDirectoryName(current);
                    Process.Start(Path.Combine(path, "ProgettoApp.exe"));
                    Thread.Sleep(200);
                }

                try
                {
                    var pipe = new System.IO.Pipes.NamedPipeClientStream("PipeSharePath");
                    pipe.Connect(1000);
                    using (var client = new System.IO.StreamWriter(pipe))
                    {
                        client.WriteLine(e.Args[0]);

                    }
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Servizio in background non in esecuzione", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("L'applicazione non accetta i parametri forniti!");
            }
            Environment.Exit(0);
        }
    }
}
