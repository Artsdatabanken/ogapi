﻿using Microsoft.AspNetCore.Mvc;
using NinMemApi.Data;
using NinMemApi.Data.Cache;
using NinMemApi.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace NinMemApi.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Produces("application/json")]
    [Route("v1/[controller]")]
    public class StatKodetreController : Controller
    {
        private static LRUCache<string, StatTreNode> _lruCache = new LRUCache<string, StatTreNode>(100);

        private readonly StatTreeBuilder _statTreeBuilder;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="statTreeBuilder"></param>
        public StatKodetreController(StatTreeBuilder statTreeBuilder)
        {
            _statTreeBuilder = statTreeBuilder;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="node">Rotnoden for treet.</param>
        /// <param name="koder">Kommaseparerte koder for filtrering.</param>
        /// <param name="bbox">For geografisk filtrering, med format: MinX,MinY,MaxX,MaxY</param>
        /// <returns></returns>
        [HttpGet]
        public StatTreNode Filtered(string node, string koder, string bbox)
        {
            node = node ?? CodeConsts.RootNodeCode;
            koder = koder ?? node;

            string cacheKey = node + "_" + koder;
            bool isBboxEmpty = string.IsNullOrWhiteSpace(bbox);

            if (isBboxEmpty && _lruCache.ContainsKey(cacheKey))
            {
                return _lruCache.Get(cacheKey);
            }

            //var codes = _codeSearch.GetNatureAreaTaxonCodesByCodesAndBbox(koder, bbox);
            var treeNode = _statTreeBuilder.Build(koder, bbox, node);

            var treNode = new StatTreNode
            {
                Kode = treeNode.Code,
                Navn = treeNode.Name,
                AntallArter = treeNode.TaxonCount,
                AntallNaturomrader = treeNode.NatureAreaCount,
                Areal = treeNode.Area,
                Forelder = treeNode.Parent != null ? new StatTreNodeForelder { Kode = treeNode.Parent.Code, Navn = treeNode.Parent.Name } : null,
                Barn = treeNode.Children.Count > 0
                  ? treeNode.Children.Values.Select(c =>
                  new StatTreNodeBarn
                  {
                      AntallArter = c.TaxonCount,
                      AntallNaturomrader = c.NatureAreaCount,
                      Areal = c.Area,
                      HarBarn = c.HasDescendants,
                      Kode = c.Code,
                      Navn = c.Name
                  }).ToList()
                  : null
            };

            if (isBboxEmpty)
            {
                _lruCache.Add(cacheKey, treNode);
            }

            return treNode;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class StatTreNode
    {
        /// <summary>
        /// 
        /// </summary>
        public StatTreNode()
        {
            Barn = new List<StatTreNodeBarn>();
        }

        /// <summary>
        /// 
        /// </summary>
        public string Kode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Navn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int AntallArter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int AntallNaturomrader { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Areal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public StatTreNodeForelder Forelder { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<StatTreNodeBarn> Barn { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class StatTreNodeBarn
    {
        /// <summary>
        /// 
        /// </summary>
        public string Kode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Navn { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int AntallArter { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int AntallNaturomrader { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double Areal { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public bool HarBarn { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class StatTreNodeForelder
    {
        /// <summary>
        /// 
        /// </summary>
        public string Kode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Navn { get; set; }
    }
}