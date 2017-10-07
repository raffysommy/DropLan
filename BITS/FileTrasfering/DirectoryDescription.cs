using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BITS.FileTrasfering
{
    public class DirectoryDescription
    {
        private List<FileDescription> _files = new List<FileDescription>();
        private List<DirectoryDescription> _directories = new List<DirectoryDescription>();
        private String _name;
        private String _username;
        private Boolean _isDirectory;
        private List<String> _cacheFilePath = new List<string>();
        /// <summary>
        /// Fill directory structure with the structure of path.
        /// </summary>
        /// <param name="path">Path directory</param>
        /// <param name="username">Sender Username</param>
        /// <param name="isDirectory">Set false if you want send only a file</param>
        public DirectoryDescription(string path,string username,bool isDirectory = true)
        {
            IsDirectory = isDirectory;
            if (IsDirectory)
            {
                Name = new DirectoryInfo(path).Name;
                Username = username;
                AddAllDirectoryFiles(path, this);
                AddAllDirectoryDirectories(path, this);
            }
            else {
                Name = "dummy";
                Username = username;
                Files.Add(new FileDescription(path));
            }
        }
        /// <summary>
        /// Costructor for subdirectory.
        /// </summary>
        /// <param name="name">Name of subdir</param>
        public DirectoryDescription(string name)
        {
            Name = name;
            Username = null;
        }
        /// <summary>
        /// Costructor with all fields.
        /// </summary>
        /// <param name="files">List of all Files</param>
        /// <param name="directories">List of all Directory</param>
        /// <param name="name">Name of folder</param>
        /// <param name="isDirectory">File or Directory flag</param>
        /// <param name="username">Sender Username</param>
        public DirectoryDescription(List<FileDescription> files, List<DirectoryDescription> directories, string name, bool isDirectory, string username)
        {
            Files = files;
            Directories = directories;
            Name = name;
            IsDirectory = isDirectory;
            Username = username;
        }
        /// <summary>
        /// Void Costructor
        /// </summary>
        public DirectoryDescription(){
        }
        /// <summary>
        /// Compute the size of directory, aggregating the size of each item.
        /// </summary>
        public Int64 Size {
            get {
                Int64 size = 0;
                if(Files!=null)
                    Files.ForEach((FileDescription fs) => { size += fs.Filesize; });
                if(Directories!=null)
                    Directories.ForEach((DirectoryDescription ds) => { size += ds.Size; });
                return size;
            }
        }
        /// <summary>
        /// Compute the total number of file in the structure.
        /// </summary>
        public Int64 FileCount
        {
            get
            {
                Int64 filecount = 0;
                filecount+=Files.Count;
                Directories.ForEach((DirectoryDescription ds) => { filecount += ds.FileCount; });
                return filecount;
            }
        }
        /// <summary>
        /// Check if the destination path contain any file that user would transfer.
        /// </summary>
        /// <param name="basepath">Destination Path</param>
        /// <returns>List of existent file</returns>
        public List<String> CheckForOverwrite(String basepath)
        {
            List<String> ExistingFile = new List<String>();
            this.Files.ForEach((f) =>
            {
                if (File.Exists(Path.Combine(basepath, f.Filename)))
                {
                    ExistingFile.Add(Path.Combine(basepath, f.Filename));
                }
            });
            this.Directories.ForEach((d) => { ExistingFile.AddRange(d.CheckForOverwrite(Path.Combine(basepath, d.Name))); });
            return ExistingFile;
        } 
        /// <summary>
        /// Scan for directory and add it recursively.
        /// </summary>
        /// <param name="path">Path of parent directory</param>
        /// <param name="ds">Parent directory structure</param>
        private void AddAllDirectoryDirectories(string path, DirectoryDescription ds)
        {
            List<string> directorylist = Directory.EnumerateDirectories(path).ToList();
            directorylist.ForEach((string directorypath) => {
                DirectoryDescription dsinner = new DirectoryDescription(new DirectoryInfo(directorypath).Name);
                AddAllDirectoryFiles(directorypath, dsinner);
                AddAllDirectoryDirectories(directorypath, dsinner);
                ds.Directories.Add(dsinner);
            });
        }
        /// <summary>
        /// Add all file in directory
        /// </summary>
        /// <param name="path">Path of parent directory</param>
        /// <param name="ds">Parent directory structure</param>
        private void AddAllDirectoryFiles(string path, DirectoryDescription ds)
        {
            List<string> filelist = Directory.EnumerateFiles(path).ToList();
            ds.Files.Capacity = filelist.Count;
            Parallel.ForEach(filelist, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (string filepath) => {
                FileDescription fd = new FileDescription(filepath);
                lock (ds.Files) {
                    ds.Files.Add(fd);
                }
            });
        }
        /// <summary>
        /// Recover complete path of all file recursively.
        /// This call can have a consistent cost so we cache it after the first call.
        /// </summary>
        /// <returns>List of all complete path</returns>
        public List<String> GetAllFilePath() {
            lock (_cacheFilePath) {
                if (_cacheFilePath.Count == 0)
                {
                    List<String> file = _cacheFilePath;
                    file.AddRange(Files.Select((f) => f.Filename));
                    Directories.ForEach((d) =>
                    {
                        file.AddRange(d.GetAllFilePath().Select((f)=>Path.Combine(d.Name,f)));
                    });
                    return file;
                }
                else
                {
                    return _cacheFilePath;
                }
            }
        }
        public String SearchForFile(Guid id) {
            FileDescription file = Files.Find((f) => f.Id == id);
            if (file==null){
                String reversepath=null;
                DirectoryDescription ds=Directories.Find(d => (reversepath=d.SearchForFile(id)) != null);
                if (ds==null&&reversepath==null)
                {
                    return null;
                }
                else {
                    return Path.Combine(Name, reversepath);
                }
            } else{
                return Name;
            }            
        }
        public List<FileDescription> Files { get => _files; set => _files = value; }
        public List<DirectoryDescription> Directories { get => _directories; set => _directories = value; }
        public string Name { get => _name; set => _name = value; }
        public bool IsDirectory { get => _isDirectory; set => _isDirectory = value; }
        public string Username { get => _username; set => _username = value; }
    }
}
