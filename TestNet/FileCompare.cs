using System;
using System.Collections.Generic;

namespace TestNet
{
    class FileCompare : IEqualityComparer<System.IO.FileInfo>
    {
        public FileCompare() { }

        public bool Equals(System.IO.FileInfo f1, System.IO.FileInfo f2)
        {
            return (f1.Name == f2.Name && f1.Length == f2.Length);
        }
        public int GetHashCode(System.IO.FileInfo fi)
        {
            return fi.Name.GetHashCode() ^ fi.Length.GetHashCode();
        }
    }
}
