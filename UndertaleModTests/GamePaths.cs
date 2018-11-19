using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;

namespace UndertaleModTests
{
    internal static class GamePaths
    {
        public static string UNDERTALE_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Undertale\data.win";
        public static string UNDERTALE_MD5 = "5903fc5cb042a728d4ad8ee9e949c6eb";
        public static string UNDERTALE_SWITCH_PATH = @"..\..\..\Test\bin\Debug\switch\game.win";
        public static string UNDERTALE_SWITCH_MD5 = "427520a97db28c87da4220abb3a334c1";
        public static string DELTARUNE_PATH = @"C:\Program Files (x86)\SURVEY_PROGRAM\data.win";
        public static string DELTARUNE_MD5 = "a88a2db3a68c714ca2b1ff57ac08a032";
    }
    
    public abstract class GameTestBase
    {
        protected readonly string path;
        protected readonly string expectedMD5;
        protected UndertaleData data;

        protected static string GenerateMD5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public GameTestBase(string path, string md5)
        {
            this.path = path;
            this.expectedMD5 = md5;
        }

        [TestInitialize]
        public void LoadData()
        {
            if (!File.Exists(path))
                Assert.Inconclusive("Unable to test, file not found: " + path);

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                string fileMD5 = GenerateMD5(fs);
                if (fileMD5 != expectedMD5)
                    Assert.Inconclusive("Unable to test, incorrect file: got " + fileMD5 + " expected " + expectedMD5);
                fs.Position = 0;

                data = UndertaleIO.Read(fs);
            }
        }
    }
}
