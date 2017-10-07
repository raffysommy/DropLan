using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using BITS.DiscoveryService;

namespace ProgettoApp
{
    /// <summary>
    /// Logica di interazione per Window1.xaml
    /// </summary>
    public partial class TrayTask : Window
    {
        Settings settingswindow;
        NotifyIcon notifyIcon = new NotifyIcon();
        private DiscoveryUserLocal disc;

        public TrayTask()
        {
            InitializeComponent();
            /*Set the position to Bottom Right of screen*/
            var desktopWorkingArea = SystemParameters.WorkArea;
            Left = desktopWorkingArea.Right - Width;
            Top = desktopWorkingArea.Bottom - Height;

            /* Notify */
            notifyIcon.Icon = Properties.Resources.tray; // Shown Icon 
            notifyIcon.Click += NotifyClick;
            /* Contex dx menu */
            ContextMenu contextMenu1 = new ContextMenu();
            MenuItem menuItem1 = new MenuItem();
            MenuItem menuItem2 = new MenuItem();
            contextMenu1.MenuItems.AddRange(new MenuItem[] { menuItem1,menuItem2 });
            menuItem1.Index = 0;
            menuItem1.Text = "Forza Uscita";
            menuItem1.Click += (args, eve) => {
                if(System.Windows.MessageBox.Show("Alcuni file potrebbero essere corrotti se forzi l'uscita.", "Dragons Ahead!", MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    Environment.Exit(0);
                }
            };
            menuItem2.Index = 1;
            menuItem2.Text = "Esci";
            menuItem2.Click += (args, eve) => { BITS.FileTrasfering.FileServer.Instance.Terminate(); Close(); };
            notifyIcon.ContextMenu = contextMenu1;

            /* Make notify icon show */
            notifyIcon.Visible = true;

            /* Register handler for file server status (show a popup) */
            BITS.FileTrasfering.FileServer.Instance._reportStatus += Instance__reportStatus;

            /* Set status according to stored setting */
            if (Properties.Settings.Default.IsPrivate)
            {
                Status_Text.Text = "Stato:\r\nNascosto";
            }
            else
            {
                Status_Text.Text = "Stato:\r\nOnline";
            }
            
            Imp_Cond.MouseUp += Imp_Cond_MouseUp;
            Status_Text.MouseUp += Status_Text_MouseUp;
            /* If we leave trayarea the task disappear after 500ms. */
            Timer t1 = new Timer();
            MouseLeave += (sender, args) => {
                t1.Interval = 500; //hide after 500ms
                t1.Start();
                t1.Tick += (send, arg) => { t1.Stop(); Hide(); };
            };
            MouseEnter += (sender, args) => t1.Stop();
        }
        /// <summary>
        /// Report transfer status on server side.
        /// </summary>
        /// <param name="ts">Transfer Status</param>
        private void Instance__reportStatus(BITS.FileTrasfering.TransferStatus ts)
        {
            notifyIcon.BalloonTipText = ts.Status;
            notifyIcon.ShowBalloonTip(1000);
        }

        public TrayTask(DiscoveryUserLocal disc):this()
        {
            this.disc = disc;
        }

        private void Status_Text_MouseUp(object sender, MouseButtonEventArgs e)
        { 
            if (!Properties.Settings.Default.IsPrivate) {
                Properties.Settings.Default.IsPrivate = true;
                disc.Privatemode = true;
                Status_Text.Text = "Stato:\r\nNascosto";
            }
            else
            {
                disc.Privatemode = false;
                Properties.Settings.Default.IsPrivate = false;
                Status_Text.Text = "Stato:\r\nOnline";
            }
            Properties.Settings.Default.Save();
        }

        private void Imp_Cond_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (settingswindow == null) {
                settingswindow = new Settings();
                settingswindow.Closed += (eve, args) => { settingswindow = null;};
            }
            if (!settingswindow.IsVisible)
            {
                settingswindow.Show();
            }
            Hide();
        }

        private void NotifyClick(object sender, EventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
            else
            {
                Topmost = true;
                Show();
            }
        }
    }
}
