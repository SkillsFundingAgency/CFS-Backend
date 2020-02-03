using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private string _currentDirectory;

        [TestInitialize]
        public void SetUp()
        {
            _files = new List<TempFile>();
            _createdFiles = new List<string>();
            _currentDirectory = Directory.GetCurrentDirectory();

            _fileSystemAccess = new FileSystemAccess();
        }

        [TestCleanup]
        public void TearDown()
        {
            foreach (TempFile tempFile in _files)
            {
                tempFile.Dispose();
            }

            foreach (string createdFile in _createdFiles)
            {
                try
                {
                    File.Delete(createdFile);
                }
                catch
                {
                }
            }
        }
        [TestMethod]
        public void DeleteRemovesTheFileAtTheSuppliedPath()
        {
            string existingFilePath = NewRandomTxtFilePath();

            GivenTheFile(existingFilePath, NewRandomString());

            WhenTheFileIsDeleted(existingFilePath);

            ThenTheFileShouldNotExist(existingFilePath);
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
        public async Task AppendWritesAllBytesCreatesNewFilesIfTheyDontExist()
        {
            string newPath = NewRandomTxtFilePath();
            string expectedFileContents = NewLongRandomString();

            await WhenTheStreamIsAppended(newPath, expectedFileContents);

            ThenTheFileContentsShouldBe(newPath, expectedFileContents);
        }
        
        [TestMethod]
        public async Task AppendAppendsAllBytesToExistingFilesIfTheyExist()
        {
            
            string existingFilePath = NewRandomTxtFilePath();
            string existingFileContents = NewLongRandomString();
            string appendedFileContents = NewLongRandomString();
            
            GivenTheFile(existingFilePath, existingFileContents);

            await WhenTheStreamIsAppended(existingFilePath, appendedFileContents);

            ThenTheFileContentsShouldBe(existingFilePath, $"{existingFileContents}{appendedFileContents}");
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
        
        [TestMethod]
        public void ListsAllFilesMatchingPredicateUnderSuppliedFolder()
        {
            string pathSegment = NewRandomString();
            
            string fileNameOne = NewRandomTxtFilePath(pathSegment);
            string fileNameTwo = NewRandomTxtFilePath();
            string fileNameThree = NewRandomTxtFilePath(pathSegment);
            
            GivenTheFile(fileNameOne);
            AndTheFile(fileNameTwo);
            AndTheFile(fileNameThree);
            
            IEnumerable<string> allFiles = _fileSystemAccess.GetAllFiles(_currentDirectory, 
                file => file.FullName.Contains(pathSegment));

            allFiles
                .Should()
                .NotBeNull();

            allFiles
                .Should()
                .BeEquivalentTo(FullPathFor(fileNameOne), FullPathFor(fileNameThree));
        }

        private string FullPathFor(string fileName) => Path.Combine(_currentDirectory, fileName);

        private async Task WhenTheStreamIsWritten(string path, string text)
        {
            await _fileSystemAccess.Write(path, new MemoryStream(GetBytes(text)), CancellationToken.None);

            _createdFiles.Add(path);
        }
        
        private async Task WhenTheStreamIsAppended(string path, string text)
        {
            await _fileSystemAccess.Append(path, new MemoryStream(GetBytes(text)), CancellationToken.None);

            _createdFiles.Add(path);
        }

        private void WhenTheFileIsDeleted(string path)
        {
            _fileSystemAccess.Delete(path);
        }

        private void GivenTheFile(string path, string contents = null)
        {
            _files.Add(new TempFile(path, contents ?? NewRandomString()));
        }

        private void AndTheFile(string path, string contents = null)
        {
            GivenTheFile(path, contents);
        }

        private void ThenTheFileShouldNotExist(string path)
        {
            File.Exists(path)
                .Should()
                .BeFalse();
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

        private static string NewRandomTxtFilePath(string pathSegment = null)
        {
            return $"{NewRandomString()}{pathSegment}.txt";
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