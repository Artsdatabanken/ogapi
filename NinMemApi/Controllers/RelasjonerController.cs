using Microsoft.AspNetCore.Mvc;
using NinMemApi.GraphDb;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NinMemApi.Controllers
{
    [Produces("application/json")]
    [Route("v1/[controller]")]
    public class RelasjonerController
    {
        private readonly ICosmosGraphClient _client;

        public RelasjonerController(ICosmosGraphClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Henter relasjoner for ressursen med angitt kode. inV: Relasjon til. outV: Relasjon fra.
        /// </summary>
        /// <param name="kode">Kode.</param>
        /// <param name="detaljert">Detaljert visning.</param>
        /// <returns></returns>
        [HttpGet]
        public Task<IEnumerable<dynamic>> Get(string kode, bool? detaljert = false)
        {
            if (string.IsNullOrWhiteSpace(kode))
            {
                throw new ArgumentException("Koden kan ikke være tom.");
            }

            string query = detaljert.Value
                ? $"g.V('{kode}').bothE().otherV().path()"
                : $"g.V('{kode}').bothE()";

            return _client.RunQuery(query);
        }
    }
}