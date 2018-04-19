using Microsoft.AspNetCore.Mvc;
using NinMemApi.Data;
using NinMemApi.Data.Models;
using System;

namespace NinMemApi.Controllers
{
    [Produces("application/json")]
    [Route("v2/[controller]")]
    public class KoderController : Controller
    {
        private readonly CodeSearch _codeSearch;

        public KoderController(CodeSearch codeSearch)
        {
            _codeSearch = codeSearch;
        }

        /// <summary>
        /// Returnerer koder for fritekstsøk.
        /// </summary>
        /// <param name="q">Mellomromseparert fritekstsøk. Minimum to tegn.</param>
        /// <param name="antall">Maksimalt antall søketreff. Hvis ikke angitt: 10. Maksgrense: 100.</param>
        /// <returns></returns>
        [HttpGet]
        public KodeNavn[] Filtered(string q, int? antall = 10)
        {
            if (antall.Value < 1 || antall.Value > 100)
            {
                throw new ArgumentException("Antall søketreff må være minimum 1 og maksimum 100.");
            }

            return _codeSearch.GetCodeNamesByFreeText(q, antall.Value);
        }
    }
}