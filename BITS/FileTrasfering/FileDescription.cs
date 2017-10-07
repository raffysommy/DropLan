using System;
using System.IO;

namespace BITS.FileTrasfering
{
    public class FileDescription
    {
        private String _filename;
        private Int64 _filesize;
        private String _filehash;
        private Guid _id;

        public string Filename { get => _filename; set => _filename = value; }
        public long Filesize { get => _filesize; set => _filesize = value; }
        public string Filehash { get => _filehash; set => _filehash = value; }
        public Guid Id { get => _id; set => _id = value; }

        /// <summary>
        /// Void Costructor
        /// </summary>
        public FileDescription()
        {
        }
        /// <summary>
        /// Create description from file structure
        /// </summary>
        /// <param name="filepath">Path of file</param>
        public FileDescription(string filepath) {
            FileInfo info = new FileInfo(filepath);
            String hashstr;
            using (FileStream stream=new FileStream(filepath,FileMode.Open,FileAccess.Read,FileShare.Read,4194304))
            {
                System.Data.HashFunction.xxHash xxhash = new System.Data.HashFunction.xxHash();
                byte[] hash = xxhash.ComputeHash(stream);
                hashstr = BitConverter.ToString(hash).Replace("-", "");
            }
            Filename = info.Name;
            Filesize = info.Length;
            Filehash=hashstr;
            Id = Guid.NewGuid();
        }
        /// <summary>
        /// Costructor with all parameters
        /// </summary>
        /// <param name="filename">Short name of the file</param>
        /// <param name="filesize">Size of the file</param>
        /// <param name="filehash">Hash of the file</param>
        /// <param name="guid">Unique id of file</param>
        public FileDescription(string filename, long filesize, string filehash,Guid guid)
        {
            Filename = filename;
            Filesize = filesize;
            Filehash = filehash;
            Id = guid;
        }
    }
}