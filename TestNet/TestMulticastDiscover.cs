using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BITS.DiscoveryService;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace TestNet
{
    [TestClass]
    public class TestMulticastDiscover
    {
        [TestMethod]
        public void MulticastDiscoverTestPrivate()
        {
            User user1 = DummyUser.Get("test1");
            UDPMulticast.MulticastLoopbackAllow = true;
            DiscoveryUserLocal ds1 = new DiscoveryUserLocal(user1, true);
            User user2 = DummyUser.Get("test2");
            UdpClient udp = new UdpClient();
            Byte[] send = Encoding.UTF8.GetBytes(user2.ToJson());
            udp.Send(send, send.Length, new IPEndPoint(IPAddress.Parse("239.0.0.222"), 3000));
            udp.Close();
            Thread.Sleep(200);
            ds1.Listuser.ForEach((User u) => { Console.WriteLine(u.Username); });
            Assert.IsTrue(ds1.Listuser.Contains(user2));
        }

        [TestMethod]
        public void MulticastDiscoverTestMultiple()
        {
            User user1 = DummyUser.Get("test1");
            UDPMulticast.MulticastLoopbackAllow = true;
            DiscoveryUserLocal ds1 = new DiscoveryUserLocal(user1);
            User user2 = DummyUser.Get("test2");
            User user3 = DummyUser.Get("test3");
            User user4 = DummyUser.Get("test4");
            UdpClient udp = new UdpClient();
            Byte[] send = Encoding.UTF8.GetBytes(user2.ToJson());
            udp.Send(send, send.Length, new IPEndPoint(IPAddress.Parse("239.0.0.222"), 3000));
            send = Encoding.UTF8.GetBytes(user3.ToJson());
            udp.Send(send, send.Length, new IPEndPoint(IPAddress.Parse("239.0.0.222"), 3000));
            send = Encoding.UTF8.GetBytes(user4.ToJson());
            udp.Send(send, send.Length, new IPEndPoint(IPAddress.Parse("239.0.0.222"), 3000));
            udp.Close();
            Thread.Sleep(1000);
            ds1.Listuser.ForEach((User u) => { Console.WriteLine(u.Username); });
            Assert.IsTrue(ds1.Listuser.Contains(user2) && ds1.Listuser.Contains(user3) && ds1.Listuser.Contains(user4));
        }
        [TestMethod]
        public void MulticastDiscoverTestMultipleTimeout()
        {
            User user1 = DummyUser.Get("test1");
            UDPMulticast.MulticastLoopbackAllow = true;
            DiscoveryUserLocal ds1 = new DiscoveryUserLocal(user1);
            User user2 = DummyUser.Get("test2");
            User user3 = DummyUser.Get("test3");
            User user4 = DummyUser.Get("test4");
            UdpClient udp = new UdpClient();
            Byte[] send = Encoding.UTF8.GetBytes(user2.ToJson());
            udp.Send(send, send.Length, new IPEndPoint(IPAddress.Parse("239.0.0.222"), 3000));
            send = Encoding.UTF8.GetBytes(user3.ToJson());
            udp.Send(send, send.Length, new IPEndPoint(IPAddress.Parse("239.0.0.222"), 3000));
            send = Encoding.UTF8.GetBytes(user4.ToJson());
            udp.Send(send, send.Length, new IPEndPoint(IPAddress.Parse("239.0.0.222"), 3000));
            udp.Close();
            Thread.Sleep(70000);
            ds1.Listuser.ForEach((User u) => { Console.WriteLine(u.Username); });
            Assert.IsTrue(!ds1.Listuser.Contains(user2) && !ds1.Listuser.Contains(user3) && !ds1.Listuser.Contains(user4));
        }
    }
}
