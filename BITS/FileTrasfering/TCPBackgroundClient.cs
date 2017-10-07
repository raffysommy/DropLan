using System;
using System.Net.Sockets;
using BITS.DiscoveryService;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net;

namespace BITS.FileTrasfering
{
    delegate void UpdateByteInner(Int64 ByteTransfered);
    class TCPBackgroundClient
    {
        public event UpdateByteInner _updateByteInner;
        TcpClient tcpc;
        /// <summary>
        /// Background connection for Tcp Client
        /// </summary>
        /// <param name="remote">Remote endpoint</param>
        public TCPBackgroundClient(IPEndPoint remote)
        {
            tcpc = new TcpClient();
            tcpc.Connect(remote);
            
        }
        /// <summary>
        /// Transfer the directory structure description
        /// </summary>
        /// <param name="ds">Directory Description</param>
        public void TransferDirDescr(DirectoryDescription ds) {
            StreamWriter sw = new StreamWriter(tcpc.GetStream());
            sw.WriteLine(JsonConvert.SerializeObject(ds));
            sw.Flush();
            StreamReader sr = new StreamReader(tcpc.GetStream());
            TransferStatus ts= JsonConvert.DeserializeObject<TransferStatus>(sr.ReadLine());
            ts.IsOk();
        }
        /// <summary>
        /// Transfer a file
        /// </summary>
        /// <param name="f">File to transfer</param>
        public void TransferFile(FileDescription fd,String path)
        {
            using (FileStream f = File.OpenRead(Path.Combine(path,fd.Filename)))
            {
                NetworkStream ns = tcpc.GetStream();
                StreamWriter sw = new StreamWriter(ns);
                sw.WriteLine(JsonConvert.SerializeObject(fd.Id));
                sw.Flush();
                StreamReader sr = new StreamReader(ns);
                try
                {
                    TransferStatus tsid = JsonConvert.DeserializeObject<TransferStatus>(sr.ReadLine());
                    tsid.IsOk();
                }
                catch(JsonException)
                {
                    throw new TransferException("L'host remoto ha inviato una risposta invalida");
                }
                BufferedStream bs = new BufferedStream(ns);
                if (fd.Filesize > 0) //handle empty file
                {
                    Task copy = f.CopyToAsync(bs);
                    Int64 position = 0;
                    while (!copy.IsCompleted)
                    {
                        Int64 tempPosition = f.Position;
                        Int64 bytetransfered = tempPosition - position;
                        _updateByteInner?.Invoke((bytetransfered));
                        position = tempPosition;
                    }
                    bs.Flush();
                }
                try
                {
                    TransferStatus ts = JsonConvert.DeserializeObject<TransferStatus>(sr.ReadLine());
                    ts.IsOk();
                }
                catch (JsonException)
                {
                    throw new TransferException("L'host remoto ha inviato una risposta invalida");
                }
            }
        }
        /// <summary>
        /// Close network connection
        /// </summary>
        internal void Close()
        {
            if(tcpc.Connected)
                tcpc.GetStream().Dispose();
            tcpc.Close();
        }
    }
}
