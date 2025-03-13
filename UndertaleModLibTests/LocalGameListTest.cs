using System.Security.Cryptography;
using UndertaleModLib;
using Xunit;

namespace UndertaleModLibTests;

public class LocalGameListTest
{
    /// <summary>
    /// List file to read WAD file paths from.
    /// </summary>
    public const string InputListFile = "game-test-list.txt";

    /// <summary>
    /// Max degree of parallelism to use. 
    /// Lower takes longer, but uses less system resources (particularly memory).
    /// </summary>
    public const int MaxParallelism = 3;

    [Fact]
    public void TestAllGamesLoadSave()
    {
        // If no game test files supplied, just ignore this test.
        if (!File.Exists(InputListFile))
        {
            return;
        }

        // Parallel options: make sure not to use too many system resources
        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = Math.Min(MaxParallelism, Environment.ProcessorCount - 1)
        };

        // Load list of file paths to test from the file
        string[] filesToTest = File.ReadAllLines(InputListFile);
        Parallel.ForEach(filesToTest, options, (string file) =>
        {
            // Ignore empty lines, and lines starting with ';' (comments)
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }
            if (file.TrimStart().StartsWith(';'))
            {
                return;
            }

            // Ensure the file actually exists
            Assert.True(File.Exists(file));

            // Load file (and also calculate MD5 of it)
            UndertaleData? data;
            byte[] originalHash;
            long originalDataSize;
            using (FileStream inputFs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Read file
                data = UndertaleIO.Read(inputFs, (string warning) =>
                {
                    throw new Exception($"Warning occurred: {warning}");
                });
                originalDataSize = inputFs.Position;

                // Calculate hash
                inputFs.Position = 0;
                using MD5 md5 = MD5.Create();
                originalHash = md5.ComputeHash(inputFs);
            }

            // Save file to memory (and calculate MD5 of it as well)
            byte[] newHash;
            long newDataSize;
            using (MemoryStream outputMs = new((int)originalDataSize))
            {
                // Write to memory stream
                UndertaleIO.Write(outputMs, data);
                newDataSize = outputMs.Position;

                // Get rid of data; no longer needed
                data = null;

                // Calculate hash
                outputMs.Position = 0;
                using MD5 md5 = MD5.Create();
                newHash = md5.ComputeHash(outputMs);
            }

            // Check to ensure the lengths and MD5s are identical
            Assert.Equal(originalDataSize, newDataSize);
            Assert.Equal(originalHash, newHash);
        });
    }
}
