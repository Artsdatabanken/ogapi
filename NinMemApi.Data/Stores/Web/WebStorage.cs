using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NinMemApi.Data.Interfaces;

namespace NinMemApi.Data.Stores.Web
{
    public class WebStorage : IStorage
    {
        private readonly string _baseUrl;

        public WebStorage(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public Task Delete(string key, string containerName = StorageConstants.NinMemApiContainerName)
        {
            throw new NotImplementedException();
        }

        public async Task<T> Get<T>(string key, string containerName = StorageConstants.NinMemApiContainerName)
        {
            var jsonBytes = await new WebClient().DownloadDataTaskAsync(new Uri(_baseUrl + key + ".json"));
            
            var jsonString = Encoding.UTF8.GetString(jsonBytes, 0, jsonBytes.Length);

            if (typeof(T) == typeof(string))
            {
                return (T)(object)jsonString;
            }

            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public Task Store<T>(string key, T value, string containerName = StorageConstants.NinMemApiContainerName)
        {
            throw new NotImplementedException();
        }
    }
}