using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;

namespace BITS.FileTrasfering
{
    public delegate string AskForPath(String resourceName,String Username);
    public delegate bool AskForFile(List<String> file, String Username);
    public delegate void ReportStatus(TransferStatus ts);

    public class FileServer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Lazy<FileServer> lazy = new Lazy<FileServer>(() => { return new FileServer(); });
        private List<DirectoryDescription> _transaction=new List<DirectoryDescription>();
        public static FileServer Instance => lazy.Value;
        TCPBackgroundServer tbs;
        public event AskForPath AskForPath;
        public event AskForFile AskForOverwrite;
        public event AskForFile AskForAccept;
        public event ReportStatus _reportStatus;
        /// <summary>
        /// Private costructor (We use a Singleton)
        /// </summary>
        private FileServer() {
            tbs = new TCPBackgroundServer(3523);
            tbs._transferRequest += Tbs__transferRequest;
        }
        /// <summary>
        /// Gracefuly terminate the receive.
        /// </summary>
        public void Terminate()
        {
            tbs.Terminate();
        }
        /// <summary>
        /// Event Handler for a transfer request
        /// </summary>
        /// <param name="sender">TcpClient connection with remote host</param>
        /// <param name="args">Directory Description Event Wrapper</param>
        private void Tbs__transferRequest(object sender, FileEventArgs.EventTransferArgs args)
        {
            String path = null;
            DirectoryDescription ds = args.Ds;
            NetworkStream ns=((TcpClient)sender).GetStream();
            /* Check if we receive a file or a directory */
            if (ds.IsDirectory)
            {
                /* Ask to the client if want accept the directory and next ask for path */
                if (AskForAccept(ds.GetAllFilePath(), ds.Username))
                    path = AskForPath?.Invoke(ds.Name, ds.Username);
            }
            else {
                /* Ask to the client if want accept the file and next ask for path  */
                if (AskForAccept(new List<string>() { ds.Files.First().Filename }, ds.Username))
                    path = AskForPath?.Invoke(ds.Files.First().Filename, ds.Username);
            }
            if (path == null || path.Equals("|||REFUSED|||"))
            {
                logger.Info("User refuse file transfer");
                using (StreamWriter swerr = new StreamWriter(ns))
                {
                    swerr.WriteLine(JsonConvert.SerializeObject(new TransferStatus("Trasferimento rifuitato")));
                }
                return;
            }
            /* Check if the path is valid and if the user have permission */
            try {
                System.Security.AccessControl.DirectorySecurity acs = Directory.GetAccessControl(path);
            }catch(Exception e){
                logger.Error("Access Error:"+e.Message);
                _reportStatus?.Invoke(new TransferStatus("Il path selezionato per il salvataggio non è accessibile"));
                using (StreamWriter swerr = new StreamWriter(ns))
                {
                    swerr.WriteLine(JsonConvert.SerializeObject(new TransferStatus("L'utente remoto ha selezionato un path invalido per il salvataggio del file!")));
                }
                throw new TransferException("Il path selezionato per il salvataggio non è accessibile");
            }

            StreamWriter sw = new StreamWriter(ns);
            /* Check if the current transfer collide with another one, and silently discard it */
            lock (_transaction)
            {
                if (_transaction.Find((t) => t.GetAllFilePath().Intersect(ds.GetAllFilePath()).Any()) != null)
                {
                    sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("File in conflitto con un altro trasferimento in corso, impossibile avviare trasferimento corrente.")));
                    sw.Flush();
                    return;
                }
                else {
                    _transaction.Add(ds);
                }
            }
            /* Check if the current file overwrite existing file and ask to user*/
            try
            {
                String pathtocheck = ds.IsDirectory ? Path.Combine(path, ds.Name) : path;
                List<String> overwritablefile = ds.CheckForOverwrite(pathtocheck);
                if (overwritablefile.Count == 0 || (AskForOverwrite?.Invoke(overwritablefile, ds.Username) ?? true))
                {
                    overwritablefile.ForEach((filename) => File.Delete(filename));
                    sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("OK")));
                    sw.Flush();
                }
                else
                {
                    sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("Alcuni file esistono già nel computer remoto e l'utente ha deciso di non sovrascriverli.")));
                    sw.Flush();
                    lock (_transaction)
                    {
                        /* Remove Transaction from list*/
                        _transaction.Remove(ds);
                    }
                    return;
                }
            }
            catch {
                lock (_transaction)
                {
                    /* Remove Transaction from list*/
                    _transaction.Remove(ds);
                }
                throw;
            }
            /* Effective Transfer */
            try
            {
                if (ds.IsDirectory)
                {
                    ReceiveDirectory(ds, path, ns);
                }
                else
                {
                    ReceiveFile(ds.Files.First(), path, ns);
                }
                _reportStatus?.Invoke(new TransferStatus("Trasferimento di: " + (ds.IsDirectory ? ds.Name : ds.Files.First().Filename) + " completato"));
            }
            catch (Exception e)
            {
                if (e is JsonException)
                {
                    _reportStatus?.Invoke(new TransferStatus("L'host remoto ha inviato una risposta invalida"));
                }
                else
                {
                    _reportStatus?.Invoke(new TransferStatus(e.Message));
                }
                /* Restore previous state if transfer not complete */
                ds.GetAllFilePath().ForEach((fpath) => {
                    try {
                        /* NB: we delete file not directory */
                        File.Delete(Path.Combine(path,ds.Name,fpath));
                    } catch {
                        /* If we cannot remove file, we just log it */
                        logger.Error("File: " + fpath + " cannot be deleted");
                    }
                });
            }
            finally{
                lock (_transaction)
                {
                    /* Remove Transaction from list*/
                    _transaction.Remove(ds);
                }
            }
        }
        /// <summary>
        /// Receive a file
        /// </summary>
        /// <param name="fileDescription">File to receive</param>
        /// <param name="path">Path where store the file</param>
        /// <param name="ns">Network Stream</param>
        private void ReceiveFile(FileDescription fileDescription,String path, NetworkStream ns)
        {
            StreamReader sr = new StreamReader(ns);
            Guid id = JsonConvert.DeserializeObject<Guid>(sr.ReadLine());
            StreamWriter sw = new StreamWriter(ns);
            if (fileDescription.Id != id)
            {
                sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("Trasferimento fuori ordine interrotto, reinvia il file")));
                sw.Flush();
                logger.Warn("Out of order Transfer: Expected id:"+fileDescription.Id.ToString()+" Received: "+id.ToString());
                throw new TransferException("Trasferimento fuori ordine interrotto, reinvia il file");              
            }
            sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("OK")));
            sw.Flush();
            string filepath=Path.Combine(path, fileDescription.Filename);
            Int64 bytetransfered=0;
            /* We open file only if not exist */
            using (FileStream filest = File.Open(filepath, FileMode.CreateNew, FileAccess.Write))
            {
                using (BufferedStream fs = new BufferedStream(filest))
                {
                    Int64 byteLeft = fileDescription.Filesize;
                    while (byteLeft > 0)
                    {
                        byte[] filebyte = new byte[4096];
                        Int32 bytetoread = (byteLeft < 4096) ? Convert.ToInt32(byteLeft) : 4096;
                        int byteread = ns.Read(filebyte, 0, bytetoread);
                        if (byteread == 0)
                        {
                            throw new TransferException("Trasferimento interrotto dall'utente remoto");
                        }
                        fs.Write(filebyte, 0, byteread);
                        byteLeft -= byteread;
                    }
                    fs.Flush();
                    bytetransfered = fs.Position;
                }
            }
            /* Controllo se ho ricevuto tutto il file */
            if (bytetransfered != fileDescription.Filesize)
            {
                logger.Warn("Filesize is not equal: Interrupt transfer");
                sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("Dimensione del file : " + fileDescription.Filename + " diversa: Transferimento interrotto")));
                sw.Flush();
                throw new TransferException("Dimensioni file: "+fileDescription.Filename+" non congrue");
            }
            /* Controllo l'hash del file */
            using (FileStream fs = File.OpenRead(filepath))
            {
                using (BufferedStream stream = new BufferedStream(fs, 4194304))
                {
                    System.Data.HashFunction.xxHash xxhash = new System.Data.HashFunction.xxHash();
                    byte[] hash = xxhash.ComputeHash(stream);
                    if (BitConverter.ToString(hash).Replace("-", "") != fileDescription.Filehash)
                    {
                        logger.Warn("File hash is not equal: Interrupt transfer");
                        sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("Hash del file : " + fileDescription.Filename + " diverso: Transferimento interrotto")));
                        sw.Flush();
                        throw new TransferException("Hash dei file: " + fileDescription.Filename + " non congrui");
                    }
                }
            }
            sw.WriteLine(JsonConvert.SerializeObject(new TransferStatus("OK")));
            sw.Flush();
        }
        /// <summary>
        /// Receive a directory  recursively.
        /// </summary>
        /// <param name="ds">Directory to receive</param>
        /// <param name="path">Destination path</param>
        /// <param name="ns">Network Stream</param>
        private void ReceiveDirectory(DirectoryDescription ds,String path, NetworkStream ns)
        {
            string newpath = Path.Combine(path, ds.Name);
            Directory.CreateDirectory(newpath);
            ds.Files.ForEach((FileDescription file) =>
            {
              ReceiveFile(file, newpath, ns);
            });
            ds.Directories.ForEach((DirectoryDescription dsin)=>{
                ReceiveDirectory(dsin,newpath,ns);
            });
        }
    }
}
