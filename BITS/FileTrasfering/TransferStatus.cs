using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BITS.FileTrasfering
{
    public class TransferStatus
    {
        String _status;

        public string Status { get => _status; set => _status = value; }
        /// <summary>
        /// Default Costructor
        /// </summary>
        /// <param name="status">Status of the transfer</param>
        public TransferStatus(String status) {
            Status = status;
        }
        /// <summary>
        /// Void Costructor
        /// </summary>
        public TransferStatus() {
        }
        /// <summary>
        /// Check if status is ok or completed
        /// </summary>
        /// <returns>True if ok, TransferException if not</returns>
        public bool IsOk(){
            if (Status.Equals("OK")||Status.Equals("Transferimento Completato")){
                return true;
            }
            throw new TransferException(Status);
        }
        /// <summary>
        /// Check if status is consistent
        /// </summary>
        /// <returns>True if status is present</returns>
        public bool Empty()
        {
            return _status==null||Status.Length == 0;
        }
    }
}
