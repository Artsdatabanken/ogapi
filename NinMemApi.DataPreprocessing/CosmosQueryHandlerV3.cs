using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.DataPreprocessing
{
    public class CosmosQueryHandlerV3 : IDisposable
    {
        private readonly int _degreeOfParallelism;
        private readonly List<Task> _tasks;
        private readonly DocumentClient _client;
        private readonly DocumentCollection _graph;

        public CosmosQueryHandlerV3(DocumentClient client, DocumentCollection graph, int degreeOfParallelism = 1)
        {
            _client = client;
            _graph = graph;

            _degreeOfParallelism = degreeOfParallelism;
            _tasks = new List<Task>(_degreeOfParallelism);
        }

        public async Task Add(string query, bool sync)
        {
            var task = RunQuery<dynamic>(_client, _graph, query);

            if (_degreeOfParallelism == 1)
            {
                await task;
            }
            else
            {
                _tasks.Add(task);

                if (sync || _tasks.Count == _degreeOfParallelism)
                {
                    await AwaitTasks();
                }
            }
        }

        public void Dispose()
        {
            using (_client) { }
        }

        public async Task Flush()
        {
            if (_tasks.Count > 0)
            {
                await AwaitTasks();
            }
        }

        private async Task AwaitTasks()
        {
            await Task.WhenAll(_tasks);
            _tasks.Clear();
        }

        private static async Task<IEnumerable<T>> RunQuery<T>(DocumentClient client, DocumentCollection graph, string gremlinQuery)
        {
            List<T> results = new List<T>();

            using (IDocumentQuery<T> query = client.CreateGremlinQuery<T>(graph, gremlinQuery))
            {
                while (query.HasMoreResults)
                {
                    foreach (T result in await query.ExecuteNextAsync<T>())
                    {
                        results.Add(result);
                    }
                }
            }

            return results;
        }
    }
}