using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BITS.FileTrasfering.FileEventArgs
{

    class EventTransferArgs:EventArgs
    {
        private DirectoryDescription _ds;
        public DirectoryDescription Ds { get => _ds;}
        public EventTransferArgs(DirectoryDescription ds)
        {
            _ds = ds;
        }
    }
}
