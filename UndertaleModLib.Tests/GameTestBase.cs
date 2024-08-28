using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Tests
{
    public abstract class GameTestBase
    {
        protected string path;
        protected string expectedMD5;
        protected UndertaleData data;

        public GameTestBase(string path, string md5)
        {
            this.path = path;
            this.expectedMD5 = md5;

            if (!File.Exists(path))
                Assert.Fail("Unable to test, file not found: " + path);

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                string fileMD5 = GenerateMD5(fs);
                if (fileMD5 != expectedMD5)
                    Assert.Fail("Unable to test, incorrect file: got " + fileMD5 + " expected " + expectedMD5);
                fs.Position = 0;

                data = UndertaleIO.Read(fs);
            }
        }

        protected static string GenerateMD5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
