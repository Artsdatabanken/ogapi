using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.GraphDb
{
    public class CosmosGraphClient : IDisposable, ICosmosGraphClient
    {
        private readonly GremlinServer _gremlinServer;
        private GremlinClient _client;
        private readonly static object _lock = new object();

        public CosmosGraphClient(string hostname, string authKey, string database, string collection)
        {
            _gremlinServer =
                new GremlinServer(
                    hostname,
                    443,
                    enableSsl: true,
                    username: "/dbs/" + database + "/colls/" + collection,
                    password: authKey);

            _client = CreateClient();
        }

        private GremlinClient CreateClient()
        {
            return new GremlinClient(_gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
        }

        public async Task<IEnumerable<dynamic>> RunQuery(string query)
        {
            IEnumerable<dynamic> result = null;

            try
            {
                result = await _client.SubmitAsync<dynamic>(query);
            }
            catch (Exception ex)
            {
                if (ex.Message == "The connection with the server was terminated abnormally"
                    || ex.Message.StartsWith("The WebSocket is in an invalid state"))
                {
                    lock (_lock)
                    {
                        try { using (_client) { } } catch { }

                        _client = CreateClient();
                    }

                    result = await _client.SubmitAsync<dynamic>(query);
                }
                else
                {
                    throw;
                }
            }

            return result;
        }

        public void Dispose()
        {
            using (_client) { }
        }
    }
}