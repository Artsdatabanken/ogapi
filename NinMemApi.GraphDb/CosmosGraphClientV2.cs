using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.GraphDb
{
    public class CosmosGraphClientV2 : IDisposable, ICosmosGraphClient
    {
        private readonly DocumentClient _client;
        private readonly string _databaseName;
        private readonly string _collectionName;
        private DocumentCollection _graph;

        public CosmosGraphClientV2(string host, string authKey, string database, string collection)
        {
            _databaseName = database;
            _collectionName = collection;

            _client = new DocumentClient(new Uri(host), authKey,
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Https,
                    RequestTimeout = new TimeSpan(1, 0, 0),
                    MaxConnectionLimit = 1000,
                    RetryOptions = new RetryOptions
                    {
                        MaxRetryAttemptsOnThrottledRequests = 10,
                        MaxRetryWaitTimeInSeconds = 60
                    }
                });
        }

        public void Dispose()
        {
            using (_client) { }
        }

        public async Task<IEnumerable<dynamic>> RunQuery(string query)
        {
            await EnsureGraphLoaded();

            List<dynamic> results = new List<dynamic>();

            using (IDocumentQuery<dynamic> q = _client.CreateGremlinQuery<dynamic>(_graph, query))
            {
                while (q.HasMoreResults)
                {
                    foreach (dynamic result in await q.ExecuteNextAsync<dynamic>())
                    {
                        results.Add(result);
                    }
                }
            }

            return results;
        }

        private async Task EnsureGraphLoaded()
        {
            if (_graph == null)
            {
                await _client.OpenAsync();

                _graph = await _client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(_databaseName),
                    new DocumentCollection { Id = _collectionName },
                    new RequestOptions { OfferThroughput = 1000 });
            }
        }
    }
}