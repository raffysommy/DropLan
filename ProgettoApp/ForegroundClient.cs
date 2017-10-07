using System;
using System.IO;
using BITS.DiscoveryService;
using System.IO.Pipes;
using System.Windows;

namespace ProgettoApp
{
    internal class ForegroundClient
    {
        private DiscoveryUserLocal disc;
        /// <summary>
        /// Foreground Client Class
        /// </summary>
        /// <param name="disc">Discovery Service</param>
        public ForegroundClient(DiscoveryUserLocal disc)
        {
            this.disc = disc;
        }
        /// <summary>
        /// Server shell extension connection Handler
        /// </summary>
        public void Accept()
        {

            while (true)
            {
                var pipe = new NamedPipeServerStream("PipeSharePath", PipeDirection.InOut, 254);
                var client = new StreamReader(pipe);
                pipe.WaitForConnection();                
                String path = client.ReadLine();
                Application.Current.Dispatcher.InvokeAsync((Action)delegate {
                    UserChoice uc = new UserChoice(path, disc);
                    uc.Show();
                });
            }
        }
    }
}