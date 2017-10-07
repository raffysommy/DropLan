using BITS.DiscoveryService;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace BITS.FileTrasfering
{
    public delegate void UpdateByte(Object sender, long ByteTransf);
    public delegate void UpdateFilename(Object sender, String filename);
    public class FileClient
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private TransferStatus _status=new TransferStatus();
        private Int64 _totalByte;
        private Int64 _byteTransf;
        private volatile String _currentFile ="";
        private TCPBackgroundClient _tbc;
        private Thread task;

        public long TotalByte { get { lock (this) return _totalByte; } private set { lock (this) _totalByte = value; } }
        public long ByteTransf { get { lock (this) return _byteTransf; } private set { lock (this) _byteTransf = value; } }
        public string CurrentFile { get { lock (_currentFile) return _currentFile; } private set { lock (_currentFile) _currentFile = value;} }
        public TransferStatus Status { get => _status; private set => _status = value; }


        public event UpdateByte _updateByte;
        public event UpdateFilename _updateFilename;

        /// <summary>
        /// Handle the trasfer of a file/directory 
        /// </summary>
        /// <param name="path">Path of file</param>
        /// <param name="dest">Recipient</param>
        public FileClient(String path,User dest)
        {
            task= new Thread(() =>
            {
                //String oldpath = Directory.GetCurrentDirectory();
                try
                {
                    IPEndPoint remote = new IPEndPoint(dest.Useraddr.Address,dest.Useraddr.Port);
                    //Directory.SetCurrentDirectory(Directory.GetParent(path).FullName);
                    //Check if we pass a path of a folder or of a directory
                    bool isDir=((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory);
                    DirectoryDescription ds;
                    if (isDir)
                    {
                        ds = new DirectoryDescription(path, User.Currentuser.Username);
                    }
                    else {
                        ds = new DirectoryDescription(path, User.Currentuser.Username,false);
                        path = Path.GetDirectoryName(path);
                    }
                    TotalByte = ds.Size;
                    _tbc=new TCPBackgroundClient(remote);
                    _tbc.TransferDirDescr(ds);
                    _tbc._updateByteInner += (bytet) => { ByteTransf += bytet; _updateByte?.Invoke(this, bytet); };
                    TransferDirectory(ds,path);
                    lock (Status)
                    {
                        Status.Status = "Transferimento Completato";
                        //Testing Purpose: Notify all thread that wait for transfer complete
                        Monitor.PulseAll(Status);
                    }
                }
                catch(Exception e) {
                    String message = e.Message;
                    if(e is JsonException)
                    {
                        message="L'host remoto ha inviato una risposta invalida";
                    }
                    logger.Error("Unable to send file/directory to client:" + e.Message);
                    lock (Status)
                    {
                        Status.Status = message;
                        //Testing Purpose: Notify all thread that wait for transfer complete
                        Monitor.PulseAll(Status);
                    }
                }
                finally { 
                    //Directory.SetCurrentDirectory(oldpath);
                    if (_tbc!= null)
                    {
                        _tbc.Close();
                    }
                }
            });
            task.Start();
        }
        /// <summary>
        /// Recursively transfer directory
        /// </summary>
        /// <param name="ds">Directory to transfer</param>

        private void TransferDirectory(DirectoryDescription ds,String path)
        {
            ds.Files.ForEach((FileDescription file) =>
            {
                TransferFile(file,path);
            });
            ds.Directories.ForEach((DirectoryDescription dsin) => {
                TransferDirectory(dsin, Path.Combine(path, dsin.Name));                
            });
        }
        /// <summary>
        /// Transfer a single file
        /// </summary>
        /// <param name="fd">File to transfer</param>
        private void TransferFile(FileDescription fd,String path)
        {
                _updateFilename?.Invoke(this,fd.Filename);
                CurrentFile = fd.Filename;
                _tbc.TransferFile(fd,path);

        }
        /// <summary>
        /// Abort Current Transfer
        /// </summary>
        public void Abort()
        {
            if (_tbc != null)
            {
                // If we have started the network transfer we can just close the socket.
                // This will result in an exception on the other endpoint that cancel the temporaney state.
                _tbc.Close();
            }
            else
            {
                // HACK: we need to stop calculating hash or creating structure.
                task.Abort();
            }
            
        }
    }
}
