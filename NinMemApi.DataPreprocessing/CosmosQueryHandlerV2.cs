using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NinMemApi.DataPreprocessing
{
    public class CosmosQueryHandlerV2
    {
        private readonly GremlinServer _gremlinServer;
        private readonly int _degreeOfParallelism;
        private readonly List<Task> _tasks;
        private readonly string _hostname;
        private readonly string _authKey;
        private readonly List<string> _queries;

        public string _database { get; }

        private readonly string _collection;

        public CosmosQueryHandlerV2(string hostname, string authKey, string database, string collection, int degreeOfParallelism = 32)
        {
            _hostname = hostname;
            _authKey = authKey;
            _database = database;
            _collection = collection;

            var gremlinServer =
                new GremlinServer(
                    hostname,
                    443,
                    enableSsl: true,
                    username: "/dbs/" + database + "/colls/" + collection,
                    password: authKey);

            _gremlinServer = gremlinServer;

            _degreeOfParallelism = degreeOfParallelism;
            _tasks = new List<Task>(_degreeOfParallelism);
            _queries = new List<string>();
        }

        public async Task Add(string query, bool sync)
        {
            _queries.Add(query);

            if (sync || _queries.Count == _degreeOfParallelism)
            {
                await RunQueries();
            }
        }

        private async Task RunQueries()
        {
            var tasks = new List<Task>(_queries.Count);
            var queriesThatFailed = _queries.ToList();

            using (var client = new GremlinClient(_gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                foreach (string q in _queries)
                {
                    tasks.Add(client.SubmitAsync<dynamic>(q));
                }

                _queries.Clear();

                await Task.WhenAll(tasks);
            }
        }

        public async Task Flush()
        {
            if (_queries.Count > 0)
            {
                await RunQueries();
            }
        }
    }
}