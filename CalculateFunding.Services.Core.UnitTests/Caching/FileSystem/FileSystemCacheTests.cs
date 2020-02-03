using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    [TestClass]
    public class FileSystemCacheTests
    {
        private IFileSystemAccess _fileSystemAccess;
        private IFileSystemCacheSettings _systemCacheSettings;

        private string _root;
        private string _prefix;

        private FileSystemCache _cache;
        
        private ICollection<TempFile> _tempFiles;

        [TestInitialize]
        public void SetUp()
        {
            _systemCacheSettings = Substitute.For<IFileSystemCacheSettings>();
            _fileSystemAccess = Substitute.For<IFileSystemAccess>();

            _root = NewRandomString();
            _prefix = NewRandomString();
            
            _systemCacheSettings.Path.Returns(_root);
            _systemCacheSettings.Prefix.Returns(_prefix);

            _cache = new FileSystemCache(_systemCacheSettings,
                _fileSystemAccess,
                Substitute.For<ILogger>());
            
            _tempFiles = new List<TempFile>();
        }
        
        [TestCleanup]
        public void TearDown()
        {
            foreach (TempFile tempFile in _tempFiles)
            {
                tempFile.Dispose();
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ExistsDelegatesToFileSystemAccess(bool expectedExistsFlag)
        {
            string path = NewRandomString();

            GivenTheExistsFlagForPath(path, expectedExistsFlag);

            _cache.Exists(new TestCacheKey(path))
                .Should()
                .Be(expectedExistsFlag);
        }

        [TestMethod]
        public void AddWritesBytesToSuppliedPathAndEnsuresFolderExists()
        {
            string key = NewRandomString();
            Stream content = new MemoryStream();
            CancellationToken cancellationToken = new CancellationToken();

            WhenTheContentIsAdded(key, content, cancellationToken, ensureFolderExists: true);

            ThenTheBytesWereWritten(key, content, cancellationToken);
            AndFolderWasCreated(Path.GetDirectoryName(CachePathForKey(key)));
        }
        
        [TestMethod]
        public void AddSwallowsFileCollisions()
        {
            string key = NewRandomString();
            Stream content = new MemoryStream();
            CancellationToken cancellationToken = new CancellationToken();
            IOException ioException = new IOException();

            GivenAddThrowsException(key, content, cancellationToken, ioException);
            AndTheExistsFlagForPath(key, true);

            WhenTheContentIsAdded(key, content, cancellationToken);
        }
        
        [TestMethod]
        public void AddThrowsIOExceptionsWhereNotCollision()
        {
            string key = NewRandomString();
            Stream content = new MemoryStream();
            CancellationToken cancellationToken = new CancellationToken();
            IOException expectedInnerException = new IOException();

            GivenAddThrowsException(key, content, cancellationToken, expectedInnerException);

            Action invocation = () => WhenTheContentIsAdded(key, content, cancellationToken);

            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage($"Unable to write content for file system cache item with key {key}");
        }

        [TestMethod]
        public void AddThrowsExceptionIfFileSystemAccessFails()
        {
            string key = NewRandomString();
            Stream content = new MemoryStream();
            CancellationToken cancellationToken = new CancellationToken();
            Exception expectedInnerException = new Exception();

            GivenAddThrowsException(key, content, cancellationToken, expectedInnerException);

            Action invocation = () => WhenTheContentIsAdded(key, content, cancellationToken);

            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage($"Unable to write content for file system cache item with key {key}");
        }

        [TestMethod]
        public void GetDelegatesToFileSystemAccess()
        {
            string key = NewRandomString();
            Stream expectedContent = new MemoryStream();

            GivenTheContentAtPath(key, expectedContent);

            Stream actualContent = WhenTheContentIsRead(key);

            actualContent
                .Should()
                .BeSameAs(expectedContent);
        }

        [TestMethod]
        public void GetThrowsExceptionIfFileSystemAccessFails()
        {
            string key = NewRandomString();
            Exception expectedInnerException = new Exception();

            GivenTheGetThrowsException(key, expectedInnerException);

            Func<Stream> invocation = () => WhenTheContentIsRead(key);

            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage($"Unable to read content for file system cache item with key {key}");
        }

        [TestMethod]
        public void EnsureFoldersExistDelegatesToFileSystemToCreateFolderStructureForCache()
        {
            EnsureFoldersExist();
            
            ThenFolderWasCreated(_root);
            AndFolderWasCreated(Path.Combine(_root, ProviderFundingFileSystemCacheKey.Folder));
            AndFolderWasCreated(Path.Combine(_root, FundingFileSystemCacheKey.Folder));
        }
        
        [TestMethod]
        public void EnsureFoldersExistOnlyCreatesMissingFolders()
        {
            GivenTheFolderExists(_root);
            
            EnsureFoldersExist();
            
            ThenFolderWasNotCreated(_root);
            AndFolderWasCreated(Path.Combine(_root, ProviderFundingFileSystemCacheKey.Folder));
            AndFolderWasCreated(Path.Combine(_root, FundingFileSystemCacheKey.Folder));
        }

        [TestMethod]
        public void EvictDeletesAllCachedFilesCreatedBeforeSuppliedDateTimeOffset()
        {
            string expectedEvictedFileOne = NewRandomString();
            string expectedEvictedFileTwo = NewRandomString();
            string expectedEvictedFileThree = NewRandomString();
            string expectedEvictedFileFour = NewRandomString();
            
            DateTimeOffset evictBefore = new RandomDateTime();
            
            GivenTheMatchingFileCreatesBefore(evictBefore, 
                expectedEvictedFileOne, 
                expectedEvictedFileTwo, 
                expectedEvictedFileThree, 
                expectedEvictedFileFour);
            
            WhenTheFilesAreEvicted(evictBefore);
            
            ThenTheFilesWereDeleted(expectedEvictedFileOne, 
                expectedEvictedFileTwo, 
                expectedEvictedFileThree, 
                expectedEvictedFileFour);
        }

        private void ThenTheFilesWereDeleted(params string[] deletedFiles)
        {
            foreach (string deletedFile in deletedFiles)
            {
                _fileSystemAccess
                    .Received(1)
                    .Delete(deletedFile);
            }
        }

        private void WhenTheFilesAreEvicted(DateTimeOffset before)
        {
            _cache.Evict(before);
        }

        private void GivenTheMatchingFileCreatesBefore(DateTimeOffset before, params string[] files)
        {
            FileInfo failsPredicateAfterBeforeDate = CreateTempFile();
            FileInfo failsPredicateOnBeforeDate = CreateTempFile();
            FileInfo passesPredicateBeforeDate = CreateTempFile();
            
            File.SetCreationTimeUtc(failsPredicateAfterBeforeDate.FullName, before.DateTime.AddMilliseconds(1));
            File.SetCreationTimeUtc(failsPredicateOnBeforeDate.FullName, before.DateTime);
            File.SetCreationTimeUtc(passesPredicateBeforeDate.FullName, before.DateTime.AddMilliseconds(-1));

            _fileSystemAccess.GetAllFiles(Arg.Is(_root), Arg.Is<Func<FileInfo, bool>>(predicate =>
                    !predicate(failsPredicateAfterBeforeDate) &&
                    !predicate(failsPredicateOnBeforeDate) &&
                    predicate(passesPredicateBeforeDate)))
                .Returns(files);
        }

        private FileInfo CreateTempFile()
        {
            TempFile tempFile = new TempFile(NewRandomString(), NewRandomString());
            
            _tempFiles.Add(tempFile);

            return tempFile.FileInfo;
        }

        private void GivenTheFolderExists(string path)
        {
            _fileSystemAccess.FolderExists(path)
                .Returns(true);
        }
        
        private void ThenFolderWasNotCreated(string path)
        {
            _fileSystemAccess
                .Received(0)
                .CreateFolder(path);
        }

        private void ThenFolderWasCreated(string path)
        {
            _fileSystemAccess
                .Received(1)
                .CreateFolder(path);
        }

        private void AndFolderWasCreated(string path)
        {
            ThenFolderWasCreated(path);
        }

        private void EnsureFoldersExist()
        {
            _cache.EnsureFoldersExist(ProviderFundingFileSystemCacheKey.Folder, FundingFileSystemCacheKey.Folder);
        }

        private void GivenAddThrowsException(string key, Stream content, CancellationToken cancellationToken, Exception exception)
        {
            _fileSystemAccess
                .Write(CachePathForKey(key), content, cancellationToken)
                .Throws(exception);
        }

        private void GivenTheContentAtPath(string key, Stream content)
        {
            _fileSystemAccess.OpenRead(CachePathForKey(key))
                .Returns(content);
        }

        private void GivenTheGetThrowsException(string key, Exception exception)
        {
            _fileSystemAccess
                .OpenRead(CachePathForKey(key))
                .Throws(exception);
        }

        private Stream WhenTheContentIsRead(string key)
        {
            return _cache.Get(new TestCacheKey(key));
        }

        private void WhenTheContentIsAdded(string key, Stream content, CancellationToken cancellationToken, bool ensureFolderExists = false)
        {
            _cache.Add(new TestCacheKey(key), content, cancellationToken, ensureFolderExists);
        }

        private void ThenTheBytesWereWritten(string key, Stream content, CancellationToken cancellationToken)
        {
            _fileSystemAccess
                .Received(1)
                .Write(CachePathForKey(key), content, cancellationToken);
        }

        private string CachePathForKey(string key)
        {
            return Path.Combine(_root, $"{_prefix}_{new TestCacheKey(key).Path}");
        }

        private void GivenTheExistsFlagForPath(string path, bool flag)
        {
            _fileSystemAccess.Exists(FullPathFor(path))
                .Returns(flag);
        }
        
        private void AndTheExistsFlagForPath(string path, bool flag)
        {
            GivenTheExistsFlagForPath(path, flag);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }

        private class TestCacheKey : FileSystemCacheKey
        {
            public TestCacheKey(string key) : base(key)
            {
            }

            public override string Path => Key;
        }

        private string FullPathFor(string path) => Path.Combine(_root, $"{_prefix}_{path}");
    }
}