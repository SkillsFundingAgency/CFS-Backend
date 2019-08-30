using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    [TestClass]
    public class FileSystemAccessTests
    {
        private ICollection<TempFile> _files;
        private ICollection<string> _createdFiles;

        private FileSystemAccess _fileSystemAccess;

        [TestInitialize]
        public void SetUp()
        {
            _files = new List<TempFile>();
            _createdFiles = new List<string>();

            _fileSystemAccess = new FileSystemAccess();
        }

        [TestCleanup]
        public void TearDown()
        {
            foreach (TempFile tempFile in _files)
                tempFile.Dispose();

            foreach (string createdFile in _createdFiles)
                try
                {
                    File.Delete(createdFile);
                }
                catch
                {
                }
        }

        [TestMethod]
        public void WriteFailsIfFileAlreadyExists()
        {
            string existingFilePath = NewRandomTxtFilePath();

            GivenTheFile(existingFilePath, NewRandomString());

            Func<Task> invocation = () => WhenTheStreamIsWritten(existingFilePath, NewLongRandomString());

            invocation
                .Should()
                .Throw<IOException>()
                .WithMessage($"*{existingFilePath}' already exists.");
        }

        [TestMethod]
        public async Task WriteAllBytesCreatesNewFiles()
        {
            string newPath = NewRandomTxtFilePath();
            string expectedFileContents = NewLongRandomString();

            await WhenTheStreamIsWritten(newPath, expectedFileContents);

            ThenTheFileContentsShouldBe(newPath, expectedFileContents);
        }

        [TestMethod]
        public void OpenReadReadsContentsOfFileAtSuppliedPath()
        {
            string existingFilePath = NewRandomTxtFilePath();
            string expectedFileContents = NewLongRandomString();

            GivenTheFile(existingFilePath, expectedFileContents);

            Stream actualStream =  _fileSystemAccess.OpenRead(existingFilePath);
            byte[] actualBytes = GetBytes(actualStream);
            
            actualBytes
                .Should()
                .BeEquivalentTo(GetBytes(expectedFileContents));
        }

        [TestMethod]
        public void ExistsIsTrueWhenFileExists()
        {
            string existingFilePath = NewRandomTxtFilePath();

            GivenTheFile(existingFilePath, NewLongRandomString());

            _fileSystemAccess.Exists(existingFilePath)
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void ExistsIsFalseWhenFileDoesntExist()
        {
            _fileSystemAccess.Exists(NewRandomTxtFilePath())
                .Should()
                .BeFalse();
        }

        private async Task WhenTheStreamIsWritten(string path, string text)
        {
            await _fileSystemAccess.Write(path, new MemoryStream(GetBytes(text)), CancellationToken.None);

            _createdFiles.Add(path);
        }

        private void GivenTheFile(string path, string contents)
        {
            _files.Add(new TempFile(path, contents));
        }

        private void ThenTheFileContentsShouldBe(string path, string expectedFileContents)
        {
            File.Exists(path)
                .Should()
                .BeTrue();

            string actualFileContents = File.ReadAllText(path);

            actualFileContents
                .Should()
                .Be(expectedFileContents);
        }

        private string NewLongRandomString()
        {
            return string.Concat(
                Enumerable.Range(0, new RandomNumberBetween(10, 20)).Select(_ => $"{NewRandomString()}{Environment.NewLine}"));
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private static string NewRandomTxtFilePath()
        {
            return $"{NewRandomString()}.txt";
        }

        private class TempFile : IDisposable
        {
            private readonly string _path;

            public TempFile(string path, string contents)
            {
                _path = path;

                File.WriteAllText(path, contents);
            }

            public void Dispose()
            {
                if (!File.Exists(_path)) return;

                try
                {
                    File.Delete(_path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private byte[] GetBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        private byte[] GetBytes(Stream stream)
        {
            using (BinaryReader binaryReader = new BinaryReader(stream))
            {
                return binaryReader.ReadBytes((int) stream.Length);
            }
        }
    }
}