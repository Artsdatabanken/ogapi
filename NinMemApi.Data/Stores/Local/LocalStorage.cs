using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NinMemApi.Data.Interfaces;
using NinMemApi.Data.Models;

namespace NinMemApi.Data.Stores.Local
{
    public class LocalStorage : IStorage
    {
        private static readonly string CacheFolder = Path.GetTempPath() + "\\ninMemApiData\\";

        public Task Delete(string key, string containerName = StorageConstants.NinMemApiContainerName)
        {
            throw new NotImplementedException();
        }

        public async Task<T> Get<T>(string key, string containerName = StorageConstants.NinMemApiContainerName)
        {
            string json = null;
            var filePath = string.IsNullOrWhiteSpace(CacheFolder) ? null : GetFileName(key);

            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                json = File.ReadAllText(filePath);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)json;
            }

            return JsonConvert.DeserializeObject<T>(json);
        }


        private static string GetFileName(string key, bool createfile = false)
        {
            var fileName = Path.Combine(CacheFolder, key + ".json");
            return createfile ? fileName : File.Exists(fileName) ? fileName : throw new FileNotFoundException("File not found: " + fileName);
        }

        public async Task Store<T>(string key, T value, string containerName = StorageConstants.NinMemApiContainerName)
        {
            var file = File.Create(GetFileName(key, true));
            var fileWriter = new StreamWriter(file);

            var json = typeof(T) == typeof(string) ? value as string : JsonConvert.SerializeObject(value);

            await fileWriter.WriteAsync(json);
            fileWriter.Dispose();
        }
    }
}