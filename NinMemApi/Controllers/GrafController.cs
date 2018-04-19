using Microsoft.AspNetCore.Mvc;
using NinMemApi.GraphDb;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.Controllers
{
    [Produces("application/json")]
    [Route("v1/[controller]")]
    public class GrafController
    {
        private readonly ICosmosGraphClient _client;

        public GrafController(ICosmosGraphClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Dynamisk Gremlin-traversering av grafen. inV: Relasjon til. outV: Relasjon fra.
        /// </summary>
        /// <param name="q">Gremlin-spørring.</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<dynamic>> Get(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                throw new ArgumentException("Spørringen kan ikke være tom.");
            }

            return _client.RunQuery(q);
        }
    }
}