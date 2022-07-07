using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Rippr;

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
            var infoList = service.GetDiscInfoList("E:").GetAwaiter().GetResult();
            Assert.IsTrue(infoList.Count > 0);
        }

        [TestMethod]
        public void TestGetTempDirectoryPathForTelevision()
        {
            var service = new RippingService();
            var discInfo = getTVDiscInfo();
            var tempDirPath = String.Join(@"\", service.GetPathSchemeByType(discInfo));
            Assert.IsTrue(tempDirPath.Contains(@"Person of Interest\Season 02"));
        }

        [TestMethod]
        public void TestGetOutputPathForTelevision()
        {
            var service = new Rippr.RippingService();
            var discInfo = getTVDiscInfo();
            var tempDirPath = String.Join(@"\", service.GetPathSchemeByType(discInfo));
            var outputDir = service.getOutputPath(discInfo, tempDirPath);
            Console.WriteLine(outputDir);
            Assert.IsTrue(outputDir.Contains(@"Person of Interest\Season 02"));
        }

        private DiscInfo getTVDiscInfo()
        {
            var mediaInfo = new MediaInformation();
            mediaInfo.Runtime = "43 min";
            mediaInfo.Title = "Person of Interest";
            mediaInfo.Type = "series";
            mediaInfo.Year = "2011";
            mediaInfo.SeasonNumber = 2;
            mediaInfo.EpisodeStart = 5;
            mediaInfo.EpisodeEnd = 8;
            var discInfo = new DiscInfo();
            discInfo.DiscType = "DVD";
            discInfo.DriveLetter = "E:";
            discInfo.MediaInfo = mediaInfo;
            return discInfo;
        }
    }
}
