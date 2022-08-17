using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cosmos.BlobService;
using Cosmos.BlobService.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cosmos.Tests
{
    [TestClass]
    public class CORE_E00_StorageContext_Tests
    {
        private const string FileRoot = "files";
        private static Utilities utils;

        private static readonly List<FolderFilePair> FolderPaths = new()
        {
            new()
            {
                FolderName = "folder1",
                Folder = $"{FileRoot}/folder1",
                FileName = "folder.stubxx"
            },
            new()
            {
                FolderName = "folder2",
                Folder = $"{FileRoot}/folder2",
                FileName = "folder.stubxx"
            },
            new()
            {
                FolderName = "subfolderA",
                Folder = $"{FileRoot}/folder2/subfolderA",
                FileName = "folder.stubxx"
            },
            new()
            {
                FolderName = "folder3",
                Folder = $"{FileRoot}/folder3",
                FileName = "folder.stubxx"
            }
        };

        private static StorageContext GetStorageContext()
        {
            return utils.GetStorageContext();
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            utils = new Utilities();
            var service = GetStorageContext();
            var task = service.DeleteFolderAsync("");
            task.Wait();
        }

        [TestMethod]
        public async Task A01_CreateFolders()
        {
            var service = GetStorageContext();

            service.CreateFolder(FolderPaths[0].Folder);
            service.CreateFolder(FolderPaths[1].Folder);
            service.CreateFolder(FolderPaths[2].Folder);

            var blobList = await service.GetObjectsAsync("");

            Assert.AreEqual(1, blobList.Count);

            foreach (var item in blobList) Assert.IsTrue(item.IsDirectory);
        }

        [TestMethod]
        public async Task A02_UploadFiles()
        {
            var storageContext = GetStorageContext();
            //var client = GetBlobServiceClient();
            var testFile0 = utils.GetFormFile("helloworld0.txt");
            var testFile1 = utils.GetFormFile("helloworld1.txt");
            var testFile2 = utils.GetFormFile("helloworld2.txt");

            //var fileBrowserEntry0 = await blobService.UploadFile<FileBrowserEntry>(FolderPaths[0].Folder, testFile0);
            //var fileBrowserEntry1 = await blobService.UploadFile<BlobClient>(FolderPaths[1].Folder, testFile1);
            //var fileBrowserEntry2 = await blobService.UploadFile<FileBrowserEntry>(FolderPaths[2].Folder, testFile2);

            await using (var memoryStream = new MemoryStream())
            {
                await testFile0.OpenReadStream().CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                storageContext.AppendBlob(memoryStream, new FileUploadMetaData
                {
                    ChunkIndex = 0,
                    ContentType = "text/plain",
                    FileName = "helloworld0.txt",
                    RelativePath = $"{FolderPaths[0].Folder}/helloworld0.txt",
                    TotalFileSize = memoryStream.Length,
                    UploadUid = Guid.NewGuid().ToString(),
                    TotalChunks = 1
                });
            }

            await using (var memoryStream = new MemoryStream())
            {
                await testFile1.OpenReadStream().CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                storageContext.AppendBlob(memoryStream, new FileUploadMetaData
                {
                    ChunkIndex = 0,
                    ContentType = "text/plain",
                    FileName = "helloworld1.txt",
                    RelativePath = $"{FolderPaths[1].Folder}/helloworld1.txt",
                    TotalFileSize = memoryStream.Length,
                    UploadUid = Guid.NewGuid().ToString(),
                    TotalChunks = 1
                });
            }

            await using (var memoryStream = new MemoryStream())
            {
                await testFile2.OpenReadStream().CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                storageContext.AppendBlob(memoryStream, new FileUploadMetaData
                {
                    ChunkIndex = 0,
                    ContentType = "text/plain",
                    FileName = "helloworld2.txt",
                    RelativePath = $"{FolderPaths[2].Folder}/helloworld2.txt",
                    TotalFileSize = memoryStream.Length,
                    UploadUid = Guid.NewGuid().ToString(),
                    TotalChunks = 1
                });
            }


            // Interrogate upload results;
            Assert.IsTrue(await storageContext.BlobExistsAsync($"{FolderPaths[0].Folder}/helloworld0.txt"));
            Assert.IsTrue(await storageContext.BlobExistsAsync($"{FolderPaths[1].Folder}/helloworld1.txt"));
            Assert.IsTrue(await storageContext.BlobExistsAsync($"{FolderPaths[2].Folder}/helloworld2.txt"));
        }

        [TestMethod]
        public async Task A03_ReadAndCompareFiles_AmazonAsPrimary()
        {
            var storageContext = utils.GetStorageContext_AmazonPrimary();

            var testFile0 = utils.GetFormFile("helloworld0.txt");
            var testFile1 = utils.GetFormFile("helloworld1.txt");
            var testFile2 = utils.GetFormFile("helloworld2.txt");

            var blobPath0 = $"{FolderPaths[0].Folder}/helloworld0.txt";
            var blobPath1 = $"{FolderPaths[1].Folder}/helloworld1.txt";
            var blobPath2 = $"{FolderPaths[2].Folder}/helloworld2.txt";

            // Interrogate upload results;
            Assert.IsTrue(await storageContext.BlobExistsAsync(blobPath0));
            Assert.IsTrue(await storageContext.BlobExistsAsync(blobPath1));
            Assert.IsTrue(await storageContext.BlobExistsAsync(blobPath2));

            using (var stream = await storageContext.OpenBlobReadStreamAsync(blobPath0))
            {
                Assert.AreEqual(testFile0.Length, stream.Length);
            }

            using (var stream = await storageContext.OpenBlobReadStreamAsync(blobPath1))
            {
                Assert.AreEqual(testFile1.Length, stream.Length);
            }

            using (var stream = await storageContext.OpenBlobReadStreamAsync(blobPath2))
            {
                Assert.AreEqual(testFile2.Length, stream.Length);
            }
        }

        [TestMethod]
        public async Task A03_ReadAndCompareFiles_AzureAsPrimary()
        {
            var testFile0 = utils.GetFormFile("helloworld0.txt");
            var testFile1 = utils.GetFormFile("helloworld1.txt");
            var testFile2 = utils.GetFormFile("helloworld2.txt");

            var storageContext = utils.GetStorageContext_AzurePrimary();

            using (var stream =
                await storageContext.OpenBlobReadStreamAsync($"{FolderPaths[0].Folder}/helloworld0.txt"))
            {
                Assert.AreEqual(testFile0.Length, stream.Length);
            }

            using (var stream =
                await storageContext.OpenBlobReadStreamAsync($"{FolderPaths[1].Folder}/helloworld1.txt"))
            {
                Assert.AreEqual(testFile1.Length, stream.Length);
            }

            using (var stream =
                await storageContext.OpenBlobReadStreamAsync($"{FolderPaths[2].Folder}/helloworld2.txt"))
            {
                Assert.AreEqual(testFile2.Length, stream.Length);
            }
        }

        [TestMethod]
        public async Task A05_DeleteFoldersAndBlobs()
        {
            var storageContext = GetStorageContext();

            storageContext.DeleteFile($"{FolderPaths[1].Folder}/helloworld1.txt");

            Assert.IsFalse(await storageContext.BlobExistsAsync($"{FolderPaths[1].Folder}/helloworld1.txt"));

            await storageContext.DeleteFolderAsync(FolderPaths[2].Folder);

            var folder = await storageContext.GetObjectsAsync(FolderPaths[2].Folder);

            // Folder should be gone.
            Assert.AreEqual(0, folder.Count);
        }
    }

    public class FolderFilePair
    {
        public string Folder { get; set; }
        public string FileName { get; set; }
        public string FolderName { get; set; }
    }
}