using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BITS.FileTrasfering
{
    delegate void TransferRequest(object sender, FileEventArgs.EventTransferArgs args);
    class TCPBackgroundServer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private TcpListener _tcps;
        public event TransferRequest _transferRequest;
        private List<Thread> _tclientlist;
        private Thread _tserver;
        private volatile bool _terminate = false;
        /// <summary>
        /// Handle new connection from Client
        /// </summary>
        /// <param name="tcpc">TCP client</param>
        private void TCPClientConnection(TcpClient tcpc) {
            IPEndPoint ipend = (IPEndPoint)tcpc.Client.RemoteEndPoint;
            if (ipend == null) {
                logger.Error("TCPClientConnection: IPEndPoint Invalid");
                tcpc.Close();
                return;
            }
            else
            {
                logger.Info("Received TCPClientConnection request from:" + ipend.Address.ToString());
                DirectoryDescription sendedDirectory;
                tcpc.GetStream().ReadTimeout = 60000; //avoid to handle hangup connection
                StreamReader sr = new StreamReader(tcpc.GetStream());
                try
                {
                    sendedDirectory = JsonConvert.DeserializeObject<DirectoryDescription>(sr.ReadLine());
                }
                catch (JsonException e)
                {
                    logger.Error("JSON format error: " + e.Message);
                    //here we can JUST ignore the invalid packet and close the connection
                    sr.Close();
                    tcpc.GetStream().Dispose();
                    tcpc.Close();
                    return;
                }
                _transferRequest?.Invoke(tcpc,new FileEventArgs.EventTransferArgs(sendedDirectory));
                if (tcpc.Connected)
                    tcpc.GetStream().Dispose();
                tcpc.Close();
                lock (_tclientlist)
                {
                    if (!_terminate)
                    {
                        _tclientlist.Remove(Thread.CurrentThread);
                    }
                }
            }
        }
        /// <summary>
        /// Chiudo il server e attendo che tutti i figli connessi con i client terminino*
        /// </summary>
        public void Terminate()
        {
            _terminate = true;
            _tcps.Stop();
            _tclientlist.ForEach((t) => { t.Join();});
        }
        /// <summary>
        /// Avvio un nuovo server in ascolto
        /// </summary>
        /// <param name="port">Porta in ascolto</param>
        public TCPBackgroundServer(int port) {
            _tclientlist = new List<Thread>();
            _tserver = new Thread(() => {
                while (!_terminate)
                {
                    try
                    {
                        _tcps = new TcpListener(IPAddress.Any, port);
                        _tcps.Start();
                        logger.Info("TCP Server Start");
                        while (true)
                        {

                            TcpClient tcpc = _tcps.AcceptTcpClient();
                            Thread t = new Thread(() => {
                                try { TCPClientConnection(tcpc);}
                                catch(Exception e)
                                {
                                    logger.Warn(e); //just log avoid to propagate client eviction
                                }
                                });
                            t.Start();
                            lock (_tclientlist)
                            {
                                _tclientlist.Add(t);
                            }

                        }
                    }
                    catch
                    {
                        if (_terminate)
                            break;
                        throw;
                    }
                }
            });
            _tserver.Start();
        }
    }
}
