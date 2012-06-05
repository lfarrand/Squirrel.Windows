﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Moq;
using NSync.Client;
using NSync.Core;
using NSync.Tests.TestHelpers;
using Xunit;

namespace NSync.Tests.Client
{
    public class UpdateManagerTests
    {
        public class CheckForUpdateTests
        {
            [Fact]
            public void NewReleasesShouldBeDetected()
            {
                string localReleasesFile = Path.Combine(".", "theApp", "packages", "RELEASES");

                var fileInfo = new Mock<FileInfoBase>();
                fileInfo.Setup(x => x.OpenRead())
                    .Returns(File.OpenRead(IntegrationTestHelper.GetPath("fixtures", "RELEASES-OnePointOh")));

                var fs = new Mock<IFileSystemFactory>();
                fs.Setup(x => x.GetFileInfo(localReleasesFile)).Returns(fileInfo.Object);

                var urlDownloader = new Mock<IUrlDownloader>();
                var dlPath = IntegrationTestHelper.GetPath("fixtures", "RELEASES-OnePointOne");
                urlDownloader.Setup(x => x.DownloadUrl(It.IsAny<string>()))
                    .Returns(Observable.Return(File.ReadAllText(dlPath, Encoding.UTF8)));

                var fixture = new UpdateManager("http://lol", "theApp", ".", fs.Object, urlDownloader.Object);
                var result = fixture.CheckForUpdate().First();

                Assert.NotNull(result);
                Assert.Equal(1, result.Version.Major);
                Assert.Equal(1, result.Version.Minor);
            }

            [Fact]
            public void NoLocalReleasesFileMeansWeStartFromScratch()
            {
                string localPackagesDir = Path.Combine(".", "theApp", "packages");
                string localReleasesFile = Path.Combine(localPackagesDir, "RELEASES");

                var fileInfo = new Mock<FileInfoBase>();
                fileInfo.Setup(x => x.Exists).Returns(false);

                var dirInfo = new Mock<DirectoryInfoBase>();
                dirInfo.Setup(x => x.Exists).Returns(true);

                var fs = new Mock<IFileSystemFactory>();
                fs.Setup(x => x.GetFileInfo(localReleasesFile)).Returns(fileInfo.Object);
                fs.Setup(x => x.CreateDirectoryRecursive(localPackagesDir)).Verifiable();
                fs.Setup(x => x.DeleteDirectoryRecursive(localPackagesDir)).Verifiable();
                fs.Setup(x => x.GetDirectoryInfo(localPackagesDir)).Returns(dirInfo.Object);

                var urlDownloader = new Mock<IUrlDownloader>();
                var dlPath = IntegrationTestHelper.GetPath("fixtures", "RELEASES-OnePointOne");
                urlDownloader.Setup(x => x.DownloadUrl(It.IsAny<string>()))
                    .Returns(Observable.Return(File.ReadAllText(dlPath, Encoding.UTF8)));

                var fixture = new UpdateManager("http://lol", "theApp", ".", fs.Object, urlDownloader.Object);
                fixture.CheckForUpdate().First();

                fs.Verify(x => x.CreateDirectoryRecursive(localPackagesDir), Times.Once());
                fs.Verify(x => x.DeleteDirectoryRecursive(localPackagesDir), Times.Once());
            }

            [Fact]
            public void NoLocalDirectoryMeansWeStartFromScratch()
            {
                string localPackagesDir = Path.Combine(".", "theApp", "packages");
                string localReleasesFile = Path.Combine(localPackagesDir, "RELEASES");

                var fileInfo = new Mock<FileInfoBase>();
                fileInfo.Setup(x => x.Exists).Returns(false);

                var dirInfo = new Mock<DirectoryInfoBase>();
                dirInfo.Setup(x => x.Exists).Returns(false);

                var fs = new Mock<IFileSystemFactory>();
                fs.Setup(x => x.GetFileInfo(localReleasesFile)).Returns(fileInfo.Object);
                fs.Setup(x => x.CreateDirectoryRecursive(It.IsAny<string>())).Verifiable();
                fs.Setup(x => x.GetDirectoryInfo(localPackagesDir)).Returns(dirInfo.Object);

                var urlDownloader = new Mock<IUrlDownloader>();
                var dlPath = IntegrationTestHelper.GetPath("fixtures", "RELEASES-OnePointOne");
                urlDownloader.Setup(x => x.DownloadUrl(It.IsAny<string>()))
                    .Returns(Observable.Return(File.ReadAllText(dlPath, Encoding.UTF8)));

                var fixture = new UpdateManager("http://lol", "theApp", ".", fs.Object, urlDownloader.Object);
                fixture.CheckForUpdate().First();

                fs.Verify(x => x.CreateDirectoryRecursive(localPackagesDir), Times.Once());
            }

            [Fact]
            public void CorruptedReleaseFileMeansWeStartFromScratch()
            {
                string localPackagesDir = Path.Combine(".", "theApp", "packages");
                string localReleasesFile = Path.Combine(localPackagesDir, "RELEASES");

                var fileInfo = new Mock<FileInfoBase>();
                fileInfo.Setup(x => x.Exists).Returns(true);
                fileInfo.Setup(x => x.OpenRead())
                    .Returns(new MemoryStream(Encoding.UTF8.GetBytes("lol this isn't right")));

                var dirInfo = new Mock<DirectoryInfoBase>();
                dirInfo.Setup(x => x.Exists).Returns(true);

                var fs = new Mock<IFileSystemFactory>();
                fs.Setup(x => x.GetFileInfo(localReleasesFile)).Returns(fileInfo.Object);
                fs.Setup(x => x.CreateDirectoryRecursive(localPackagesDir)).Verifiable();
                fs.Setup(x => x.DeleteDirectoryRecursive(localPackagesDir)).Verifiable();
                fs.Setup(x => x.GetDirectoryInfo(localPackagesDir)).Returns(dirInfo.Object);

                var urlDownloader = new Mock<IUrlDownloader>();
                var dlPath = IntegrationTestHelper.GetPath("fixtures", "RELEASES-OnePointOne");
                urlDownloader.Setup(x => x.DownloadUrl(It.IsAny<string>()))
                    .Returns(Observable.Return(File.ReadAllText(dlPath, Encoding.UTF8)));

                var fixture = new UpdateManager("http://lol", "theApp", ".", fs.Object, urlDownloader.Object);
                fixture.CheckForUpdate().First();

                fs.Verify(x => x.CreateDirectoryRecursive(localPackagesDir), Times.Once());
                fs.Verify(x => x.DeleteDirectoryRecursive(localPackagesDir), Times.Once());
            }

            [Fact]
            public void CorruptRemoteFileShouldThrowOnCheck()
            {
                string localPackagesDir = Path.Combine(".", "theApp", "packages");
                string localReleasesFile = Path.Combine(localPackagesDir, "RELEASES");

                var fileInfo = new Mock<FileInfoBase>();
                fileInfo.Setup(x => x.Exists).Returns(false);

                var dirInfo = new Mock<DirectoryInfoBase>();
                dirInfo.Setup(x => x.Exists).Returns(true);

                var fs = new Mock<IFileSystemFactory>();
                fs.Setup(x => x.GetFileInfo(localReleasesFile)).Returns(fileInfo.Object);
                fs.Setup(x => x.CreateDirectoryRecursive(localPackagesDir)).Verifiable();
                fs.Setup(x => x.DeleteDirectoryRecursive(localPackagesDir)).Verifiable();
                fs.Setup(x => x.GetDirectoryInfo(localPackagesDir)).Returns(dirInfo.Object);

                var urlDownloader = new Mock<IUrlDownloader>();
                urlDownloader.Setup(x => x.DownloadUrl(It.IsAny<string>()))
                    .Returns(Observable.Return("lol this isn't right"));

                var fixture = new UpdateManager("http://lol", "theApp", ".", fs.Object, urlDownloader.Object);

                Assert.Throws<Exception>(() => fixture.CheckForUpdate().First());
            }
        }

        public class DownloadReleasesTests
        {
            [Fact]
            public void ChecksumShouldPassOnValidPackages()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void ChecksumShouldFailIfFilesAreMissing()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void ChecksumShouldFailIfFilesAreBogus()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void DownloadReleasesFromHttpServerIntegrationTest()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void DownloadReleasesFromFileDirectoryIntegrationTest()
            {
                throw new NotImplementedException();
            }
        }
    }
}
