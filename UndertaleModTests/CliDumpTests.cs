using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UndertaleModTests
{
    [TestClass]
    public class CliDumpTests
    {
        [TestMethod]
        public void ReserveUniqueSoundDestinationPathAddsSuffixForDuplicateNames()
        {
            string directory = Path.Combine(Path.GetTempPath(), $"umt-cli-dump-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            try
            {
                HashSet<string> reservedPaths = new(StringComparer.OrdinalIgnoreCase);

                string firstPath = UndertaleModCli.Program.ReserveUniqueSoundDestinationPath(
                    directory,
                    "snd_duplicate",
                    ".ogg",
                    reservedPaths);
                string secondPath = UndertaleModCli.Program.ReserveUniqueSoundDestinationPath(
                    directory,
                    "snd_duplicate",
                    ".ogg",
                    reservedPaths);

                Assert.AreEqual(Path.Join(directory, "snd_duplicate.ogg"), firstPath);
                Assert.AreEqual(Path.Join(directory, "snd_duplicate_1.ogg"), secondPath);
                Assert.AreEqual(2, reservedPaths.Count);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
