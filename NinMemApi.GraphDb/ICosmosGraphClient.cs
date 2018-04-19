using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.GraphDb
{
    public interface ICosmosGraphClient
    {
        Task<IEnumerable<dynamic>> RunQuery(string query);
    }
}