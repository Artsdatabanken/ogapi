using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NinMemApi.Data.Interfaces;
using NinMemApi.Data.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace NinMemApi.Data.Stores.Azure
{
    public class AzureStorage : IStorage
    {
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, CloudBlobContainer> _containers;
        private readonly string _cacheFolder;

        public AzureStorage(ArtsdbStorageConnectionString artsdbStorageConnectionString, string cacheFolder = null)
        {
            _connectionString = artsdbStorageConnectionString.ConnectionString;
            _containers = new ConcurrentDictionary<string, CloudBlobContainer>();
            _cacheFolder = cacheFolder;
        }

        public async Task Store<T>(string key, T value, string containerName = StorageConstants.NinMemApiContainerName)
        {
            string str = typeof(T) == typeof(String) ? value as string : JsonConvert.SerializeObject(value);

            var blobReference = await GetBlockBlobReference(key, containerName);

            if (await blobReference.ExistsAsync())
            {
                throw new ArgumentException($"The blob '{key}' already exists");
            }

            await blobReference.UploadTextAsync(str);
        }

        public async Task<T> Get<T>(string key, string containerName = StorageConstants.NinMemApiContainerName)
        {
            string json = null;
            string filePath = string.IsNullOrWhiteSpace(_cacheFolder) ? null : Path.Combine(_cacheFolder, key + ".json");

            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                json = File.ReadAllText(filePath);
            }
            else
            {
                var blobReference = await GetBlockBlobReference(key, containerName);

                json = await blobReference.DownloadTextAsync();

                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    if (!Directory.Exists(_cacheFolder))
                    {
                        Directory.CreateDirectory(_cacheFolder);
                    }
                    File.WriteAllText(filePath, json);
                }
            }

            if (typeof(T) == typeof(String))
            {
                return (T)(object)json;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task Delete(string key, string containerName = StorageConstants.NinMemApiContainerName)
        {
            var blobReference = await GetBlockBlobReference(key, containerName);

            await blobReference.DeleteIfExistsAsync();
        }

        private async Task<CloudBlockBlob> GetBlockBlobReference(string key, string containerName)
        {
            var container = await GetContainer(containerName);

            return container.GetBlockBlobReference(key);
        }

        private async Task<CloudBlobContainer> GetContainer(string containerName)
        {
            if (!_containers.ContainsKey(containerName))
            {
                var storageAccount = CloudStorageAccount.Parse(_connectionString);

                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(containerName);

                //string sas = container.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                //{
                //    SharedAccessStartTime = DateTime.UtcNow,
                //    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                //    Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Read
                //});
                await container.CreateIfNotExistsAsync();

                await container.SetPermissionsAsync(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });

                _containers.TryAdd(containerName, container);
            }

            return _containers[containerName];
        }
    }
}