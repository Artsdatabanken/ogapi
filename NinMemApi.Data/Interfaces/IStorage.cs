using NinMemApi.Data.Stores;
using System.Threading.Tasks;

namespace NinMemApi.Data.Interfaces
{
    public interface IStorage
    {
        Task Delete(string key, string containerName = StorageConstants.NinMemApiContainerName);

        Task<T> Get<T>(string key, string containerName = StorageConstants.NinMemApiContainerName);

        Task Store<T>(string key, T value, string containerName = StorageConstants.NinMemApiContainerName);
    }
}