using BITS.DiscoveryService;
using BITS.FileTrasfering;
using NLog;
using Ookii.Dialogs.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;

namespace ProgettoApp
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        void App_Startup(object sender, StartupEventArgs e)
        {
            if(System.Diagnostics.Process.GetProcessesByName(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1){
                logger.Error("Another instance of same app is running.");
                Environment.Exit(0);
            }
            if (e.Args.Length == 0){
                try
                {
                    DiscoveryUserLocal disc = new DiscoveryUserLocal(FillCurrentUser(), ProgettoApp.Properties.Settings.Default.IsPrivate);
                    Thread ForegroundClientListner = new Thread(() => { new ForegroundClient(disc).Accept(); })
                    {
                        IsBackground = true
                    };
                    ForegroundClientListner.Start();
                    FileServer.Instance.AskForPath += Instance_askForPath;
                    FileServer.Instance.AskForAccept += Instance_askForAccept;
                    FileServer.Instance.AskForOverwrite += Instance_askForOverwrite;
                    TrayTask trayTask = new TrayTask(disc);
                }catch(Exception ex)
                {
                    logger.Error(ex,"General Exception" + ex.Message);
                    throw;
                }
            }
            else
            {
                MessageBox.Show("L'applicazione non accetta i parametri forniti!");
            }            
        }

        private bool Instance_askForAccept(System.Collections.Generic.List<string> file, string Username)
        {
            if (ProgettoApp.Properties.Settings.Default.IsPrivate)
                return false;
            if (ProgettoApp.Properties.Settings.Default.IsAutoAccept)
                return true;
            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.WindowTitle = "Richiesta di trasferimento file";
                dialog.MainInstruction = "Richiesta di trasferimento file";
                dialog.Content = "E' stato avviato un trasferimento dall'utente " + Username + ". Per maggiori informazioni visualizza i dettagli";
                dialog.ExpandedInformation = "I seguenti file saranno trasferiti:\n " + String.Join("\n", file.Take(100));
                TaskDialogButton acceptButton = new TaskDialogButton("Accetta");
                TaskDialogButton cancelButton = new TaskDialogButton(ButtonType.Cancel);
                dialog.Buttons.Add(acceptButton);
                dialog.Buttons.Add(cancelButton);
                TaskDialogButton button = dialog.ShowDialog();
                if (button == acceptButton)
                    return true;
                else
                    return false;
            }
        }

        private bool Instance_askForOverwrite(System.Collections.Generic.List<string> fileconfl, string Username)
        {
            using (TaskDialog dialog = new TaskDialog())
            {
                dialog.WindowTitle = "Richiesta di trasferimento file esistenti";
                dialog.MainInstruction = "Richiesta di trasferimento file esistenti";
                dialog.Content = "E' stato avviato un trasferimento dall'utente "+ Username+" che contiene file già presenti all'interno del sistema. Per maggiori informazioni visualizza i dettagli";
                dialog.ExpandedInformation = "I seguenti file saranno cancellati e sovrascritti:\n " + String.Join("\n", fileconfl.Take(100));
                TaskDialogButton overwriteButton = new TaskDialogButton("Sovrascrivi");
                TaskDialogButton cancelButton = new TaskDialogButton(ButtonType.Cancel);
                dialog.Buttons.Add(overwriteButton);
                dialog.Buttons.Add(cancelButton);
                TaskDialogButton button = dialog.ShowDialog();
                if (button == overwriteButton)
                    return true;
                else
                    return false;
            }
        }

        private string Instance_askForPath(string resourceName, string Username)
        {
            if (ProgettoApp.Properties.Settings.Default.IsStorageSet)
            {
                return ProgettoApp.Properties.Settings.Default.StorageDir;
            }
            else
            {
                VistaFolderBrowserDialog folderDiag = new VistaFolderBrowserDialog()
                {
                    UseDescriptionForTitle = true,
                    Description = "Richiesta di trasferimento di: " + resourceName + " da: " + Username
                };
                if (folderDiag.ShowDialog().Value) {
                    return folderDiag.SelectedPath;
                }
                return "|||REFUSED|||";
            }        
        }

        private User FillCurrentUser()
        {
            String myUser = Environment.UserName;
            String myUserImagePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Temp\" + Environment.UserName + ".bmp";
            System.Drawing.Image myUserImage;
            if (File.Exists(myUserImagePath))
            {
                myUserImage = System.Drawing.Image.FromFile(myUserImagePath);
            }
            else
            {
                myUserImage = ProgettoApp.Properties.Resources.userdefault;
            }
            IPAddress myAddr = Dns.GetHostAddresses(Dns.GetHostName()).ToList<IPAddress>().Find((ip) => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ProgettoApp.Properties.Settings.Default.Guid.Length == 0) {
                User u = new User(myUser, myUserImage, new IPEndPoint(myAddr, 3523));
                ProgettoApp.Properties.Settings.Default.Guid = u.Id.ToString();
                ProgettoApp.Properties.Settings.Default.Save();
                return u;
            }
            else
            {
                return new User(myUser, myUserImage, new IPEndPoint(myAddr, 3523),new Guid(ProgettoApp.Properties.Settings.Default.Guid));
            }

        }
    }
}
