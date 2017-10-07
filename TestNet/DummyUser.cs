using BITS.DiscoveryService;
using System;
using System.Drawing;
using System.Net;

namespace TestNet
{
    class DummyUser
    {
        internal static User Get(String myUser,String ip="133.25.12.33",int port=3501)
        {
            Image myUserImage = Image.FromFile("userdefault.png");
            return new User(myUser, myUserImage, new IPEndPoint(IPAddress.Parse(ip), port));
        }
    }
}
