using Microsoft.AspNetCore.Mvc;
using NinMemApi.Data.Models;
using NinMemApi.GraphDb;
using NinMemApi.GraphDb.Labels;
using System.Linq;

namespace NinMemApi.Controllers
{
    [Produces("application/json")]
    [Route("v2/[controller]")]
    public class KodetreController : Controller
    {
        private readonly G _g;

        public KodetreController(G g)
        {
            _g = g;
        }

        [HttpGet]
        public KodeNavnNode Filtered(string node)
        {
            if (string.IsNullOrWhiteSpace(node))
            {
                node = CodeConsts.RootNodeCode;
            }

            Vertex vertex = _g.V(node);
            var root = new KodeNavnNode
            {
                Kode = vertex.Id,
                Navn = vertex.Name
            };

            if (vertex.Out(EL.Child).Any())
            {
                var parentVertex = vertex.Out(EL.Child).Single();

                root.Forelder = new KodeNavnForelder
                {
                    Kode = parentVertex.Id,
                    Navn = parentVertex.Name
                };
            }

            root.Barn =
                vertex.In(EL.Child)
                .Select(v => new KodeNavnBarn
                {
                    Kode = v.Id,
                    Navn = v.Name,
                    HarBarn = v.In(EL.Child).Any()
                })
                .ToArray();

            return root;
        }
    }
}