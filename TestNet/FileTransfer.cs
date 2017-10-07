using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BITS.FileTrasfering;
using BITS.DiscoveryService;
using System.IO;
using System.Threading;
using System.Linq;

namespace TestNet
{
    /// <summary>
    /// Descrizione del riepilogo per FileTransfer
    /// </summary>
    [TestClass]
    public class FileTransfer
    {
        private static FileServer fileser=FileServer.Instance;
        private User dest;
        private String destpath;
        public FileTransfer()
        {
            User.Currentuser = DummyUser.Get("utente1");
            Directory.CreateDirectory("dest");
            destpath = Path.Combine(Directory.GetCurrentDirectory(), "dest");
            fileser.AskForAccept += (List<string> file, string Username) => { return true; };
            fileser.AskForOverwrite+= (List<string> file, string Username) => { return true; };
            fileser.AskForPath += (String resourcename,String username) => { return destpath; };
            dest = DummyUser.Get("test2", "192.168.1.65", 3523);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Ottiene o imposta il contesto del test che fornisce
        ///le informazioni e le funzionalità per l'esecuzione del test corrente.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Attributi di test aggiuntivi
        //
        // È possibile utilizzare i seguenti attributi aggiuntivi per la scrittura dei test:
        //
        // Utilizzare ClassInitialize per eseguire il codice prima di eseguire il primo test della classe
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Utilizzare ClassCleanup per eseguire il codice dopo l'esecuzione di tutti i test della classe
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Utilizzare TestInitialize per eseguire il codice prima di eseguire ciascun test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Utilizzare TestCleanup per eseguire il codice dopo l'esecuzione di ciascun test
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestSingleFileTransfer()
        {
            FileClient filecli = new FileClient("userdefault.png", dest);
            lock (filecli.Status)
            {
                while (filecli.Status.Empty())
                {
                    Monitor.Wait(filecli.Status);
                }
            }
            Thread.Sleep(2000);
            Assert.IsTrue(new FileCompare().Equals(new FileInfo("userdefault.png"),new FileInfo(Path.Combine(destpath, "userdefault.png"))));
        }
        [TestMethod]
        public void TestMultipleFileTransfer()
        {
            FileClient filecli = new FileClient(@"C:\Users\Raffaele\Visual Studio 2017\Projects\ProgettoApp\TestNet\bin\Debug\Kubernetes", dest);
            lock (filecli.Status){ 
                while(filecli.Status.Empty())
                {
                    Monitor.Wait(filecli.Status);
                }
            }
            Thread.Sleep(3000);
            DirectoryInfo dir1 = new System.IO.DirectoryInfo("Kubernetes");
            DirectoryInfo dir2 = new System.IO.DirectoryInfo(@"dest\Kubernetes");  
            IEnumerable<FileInfo> list1 = dir1.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            IEnumerable<FileInfo> filteredlist1 = list1.Select(f => f).Where(f => (f.Attributes & FileAttributes.Hidden) == 0).ToList<FileInfo>();
            IEnumerable<FileInfo> list2 = dir2.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
            IEnumerable<FileInfo> filteredlist2 = list2.Select(f => f).Where(f => (f.Attributes & FileAttributes.Hidden) == 0).ToList<FileInfo>();
            filteredlist1.Except<FileInfo>(filteredlist2, new FileCompare());
            Assert.IsTrue(filteredlist1.SequenceEqual(filteredlist2, new FileCompare()));
        }
        [AssemblyCleanup]
        public static void Cleanup() {
            Directory.Delete("dest", true);
            fileser.Terminate();
        }

    }
}
