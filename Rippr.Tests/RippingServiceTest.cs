using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace RipprTests
{
    [TestClass]
    public class RippingServiceTest
    {
        [TestMethod]
        [Ignore]
        public void TestShouldCopyFilesRecursively()
        {
            var service = new Rippr.RippingService();
            service.CopyFilesRecursively(@"C:\Fraps", @"D:\Fraps");
            var dirsAfterCopy = Directory.GetFiles(@"D:\Fraps");
            Assert.IsTrue(dirsAfterCopy.Length > 1);
            // Cleanup
            Directory.Delete(@"D:\Fraps", true);
        }

        [TestMethod]
        public void TestGetInfo()
        {
            var service = new Rippr.RippingService();
            var infoList = service.GetDiscInfoList().GetAwaiter().GetResult();
            Assert.IsTrue(infoList.Count > 0);
        }

        
    }
}
