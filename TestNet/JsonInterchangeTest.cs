using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BITS.DiscoveryService;

namespace TestNet
{
    [TestClass]
    public class JSONInterchangeUser
    {
        [TestMethod]
        public void TestInterchange()
        {
            User user = DummyUser.Get("UserTest");
            String json = user.ToJson();
            User converted = new User(json);
            Assert.AreEqual(user, converted);
        }
        [TestMethod]
        public void TestInterchangeEqual()
        {
            User user = DummyUser.Get("UserTest");
            String json = user.ToJson();
            User converted = new User(json);
            Assert.IsTrue(user.Equals(converted));
        }
        [TestMethod]
        public void TestInterchangeHash()
        {
            User user = DummyUser.Get("UserTest");
            String json = user.ToJson();
            User converted = new User(json);
            Assert.IsTrue(user.GetHashCode().Equals(converted.GetHashCode()));
        }
    }
}
