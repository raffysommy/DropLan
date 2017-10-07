using BITS.DiscoveryService;
using BITS.FileTrasfering;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ProgettoApp
{
    /// <summary>
    /// Logica di interazione per UserChoice.xaml
    /// </summary>
    public partial class UserChoice : Window
    {
        private string path;
        private ObservableCollection<ImageCaption> _ImageCollection = new ObservableCollection<ImageCaption>();
        private List<String> hashUserSelected = new List<string>();
        private Int32 nTransfer = 0;
        private DiscoveryUserLocal disc;

        public UserChoice()
        {
            InitializeComponent();
            Topmost = true;
        }

        public UserChoice(string path, DiscoveryUserLocal disc) : this()
        {
            Title = "Trasferimento di: " + path;
            this.disc = disc;
            this.path = path;
            disc._effectiveUserAdd += Disc__effectiveUserAdd;
            disc._effectiveUserRemove += Disc__effectiveUserRemove;
            this.Closing += UserChoice_Closing;
            List<User> user=disc.Listuser;
            user.ForEach((User u) =>_ImageCollection.Add(new ImageCaption(u)));
            listView.ItemsSource = _ImageCollection;
        }

        private void UserChoice_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            disc._effectiveUserAdd -= Disc__effectiveUserAdd;
            disc._effectiveUserRemove -= Disc__effectiveUserRemove;
        }

        private void Disc__effectiveUserRemove(User user)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                _ImageCollection.Remove(_ImageCollection.Where((u) => u.HashButton == user.GetHashCode().ToString()).FirstOrDefault());
            });
        }

        private void Disc__effectiveUserAdd(User user)
        {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                _ImageCollection.Add(new ImageCaption(user));
            });
        }

        private void User_Checked(object sender, RoutedEventArgs e)
        {
            hashUserSelected.Add(((CheckBox)sender).Uid);
        }
        private void User_Unchecked(object sender, RoutedEventArgs e)
        {
            hashUserSelected.Remove(((CheckBox)sender).Uid);
        }

        private void Condividi_Click(object sender, RoutedEventArgs e)
        {
            List<User> SelectedUser=disc.Listuser.FindAll((u) => { return hashUserSelected.Contains(u.GetHashCode().ToString()); });
            SelectedUser.ForEach((u) => {
                Interlocked.Increment(ref nTransfer);
                new Thread(() =>
                  {
                      ProgressDialog pd = new ProgressDialog()
                      {
                          WindowTitle = "Transferimento dati a: " + u.Username
                      };
                      pd.DoWork += UserChoice_DoWork;
                      pd.RunWorkerCompleted += Pd_RunWorkerCompleted;
                      pd.Show(u);
                }).Start();
            });
            Hide();
        }

        private void Pd_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            try
            {
                TransferStatus ts=((TransferStatus)e.Result);
                ts.IsOk();
                ((ProgressDialog)sender).Dispose();
                MessageBox.Show("Trasferimento completato con successo", "Transferimento completato");
            }
            catch (TransferException ex) {
                ((ProgressDialog)sender).Dispose();
                MessageBox.Show(ex.Message, "Transferimento interrotto", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (Interlocked.Decrement(ref nTransfer)==0){
                Application.Current.Dispatcher.InvokeAsync((Action)delegate {
                    Close();
                });
            }
        }

        private void UserChoice_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            FileClient fc = new FileClient(path,(User)e.Argument);
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            ProgressDialog pd = ((ProgressDialog)sender);
            pd.Text = "Precalcolo hash dei file";
            Double averageSpeed = 0;
            Double expFact = 0.05;
            while (fc.Status.Empty()&& !pd.CancellationPending)
            {
                if (fc.TotalByte != 0)
                {
                    if (fc.ByteTransf != 0)
                    {
                        if (fc.ByteTransf != fc.TotalByte)
                        {
                            Double actualSpeed = (double)((double)watch.ElapsedMilliseconds / (double)fc.ByteTransf);
                            averageSpeed = averageSpeed * expFact + actualSpeed * (1 - expFact);
                            Double remaingTime = averageSpeed * ((double)fc.TotalByte - (double)fc.ByteTransf);
                            Int32 prog = Convert.ToInt32(fc.ByteTransf * 100 / fc.TotalByte);
                            TimeSpan time = TimeSpan.FromMilliseconds(remaingTime);
                            pd.ReportProgress(prog, "Trasferimento in corso di: " + fc.CurrentFile, "Tempo rimanente: " + time.ToString(@"hh\:mm\:ss"));
                        }
                        else
                        {
                            pd.ReportProgress(100, "Controllo hash nel computer remoto", "Quest'operazione potrebbe richiedere un pò di tempo");
                        }

                    }
                }
                Thread.Sleep(200);
            }
            if (pd.CancellationPending)
            {
                fc.Abort();
                e.Result = new TransferStatus("Trasferimento annullato");
            }
            else
            {
                e.Result = fc.Status;
            }
        }

        private void Annulla_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
