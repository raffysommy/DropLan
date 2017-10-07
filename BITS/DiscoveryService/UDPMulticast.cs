using NLog;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BITS.DiscoveryService
{
    delegate void AsyncUserAdd(User user);
    class UDPMulticast
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private UdpClient udp;
        private IPEndPoint remote;
        internal static bool MulticastLoopbackAllow = false;
        public event AsyncUserAdd _asyncUserAdd;
        /// <summary>
        /// Multicast udp sender/receiver
        /// </summary>
        /// <param name="address">Multicast address</param>
        /// <param name="port">UDP Port</param>
        public UDPMulticast(String address, int port)
        {
            IPAddress multicastAddr = IPAddress.Parse(address);
            udp = new UdpClient();
            remote = new IPEndPoint(multicastAddr, port);
            udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            udp.JoinMulticastGroup(multicastAddr);
            udp.MulticastLoopback = false;// MulticastLoopbackAllow; //just for testing
            udp.BeginReceive(new AsyncCallback(NewHost), null);
        }
        /// <summary>
        /// Handle packet from network that announce an host
        /// </summary>
        /// <param name="ar">Packet</param>
        private void NewHost(IAsyncResult ar)
        {
            IPEndPoint sender = new IPEndPoint(0, 0);
            Byte[] received = udp.EndReceive(ar, ref sender);
            udp.BeginReceive(new AsyncCallback(NewHost), null);
            User u=new User(Encoding.UTF8.GetString(received));
            if (u.Useraddr.Address.Equals(sender.Address))
            {
                _asyncUserAdd?.Invoke(u);
            }
            else
            {
                logger.Warn("Someone try to spoof this ip:" + u.Useraddr.Address.ToString() + " from this ip:"+sender.Address.ToString());
            }
        }
        /// <summary>
        /// Announce the host presence on the network.
        /// </summary>
        /// <param name="myUser">Current user</param>
        public void SendNotification(User myUser)
        {
            Byte[] send = Encoding.UTF8.GetBytes(myUser.ToJson());
            udp.Send(send, send.Length, remote);
        }
    }
}
