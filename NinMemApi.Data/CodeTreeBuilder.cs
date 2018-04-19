using Newtonsoft.Json;
using NinMemApi.Data.Models;
using System;
using System.Collections.Generic;

namespace NinMemApi.Data
{
    public static class CodeTreeBuilder
    {
        public static CodeTreeNode Build(string json)
        {
            var flatnodes = JsonConvert.DeserializeObject<Koder>(json);

            var dict = new Dictionary<string, CodeTreeNode>();
            const string rootCode = "~";

            foreach (var flatnode in flatnodes.Data)
            {
                string code = flatnode.Kode.ToLower();

                if (!dict.TryGetValue(code, out var node))
                {
                    node = new CodeTreeNode
                    {
                        Code = !string.IsNullOrWhiteSpace(code) ? code : rootCode,
                    };

                    foreach (var kvp in flatnode.Tittel)
                    {
                        node.Names.Add(kvp.Key, kvp.Value);
                    }

                    dict.Add(node.Code, node);
                }
            }

            foreach (var flatnode in flatnodes.Data)
            {
                string code = flatnode.Kode.ToLower();
                string parent = flatnode.Forelder?.ToLower();

                if (!string.IsNullOrWhiteSpace(parent))
                {
                    var node = dict[code];

                    if (!dict.ContainsKey(parent))
                    {
                        // TODO: Log
                        //Console.WriteLine(flatnode.Forelder + " mangler!");
                        continue;
                    }

                    var forelder = dict[parent];

                    if (node.Parents.ContainsKey(forelder.Code))
                    {
                    }

                    node.Parents.Add(forelder.Code, forelder);
                    forelder.Children.Add(node.Code, node);
                }
            }

            return dict[rootCode];
        }
    }

    public class CodeTreeNode
    {
        public CodeTreeNode()
        {
            Children = new Dictionary<string, CodeTreeNode>();
            Parents = new Dictionary<string, CodeTreeNode>();
            Names = new Dictionary<string, string>();
        }

        public IDictionary<string, CodeTreeNode> Parents { get; set; }
        public IDictionary<string, CodeTreeNode> Children { get; set; }
        public string Code { get; set; }
        public string Name => Names.ContainsKey("nb") ? Names["nb"] : (Names.ContainsKey("la") ? Names["la"] : null);
        public IDictionary<string, string> Names { get; set; }

        public IDictionary<string, CodeTreeNode> GetAllDescendants()
        {
            var dict = new Dictionary<string, CodeTreeNode>();

            foreach (var child in Children)
            {
                dict.Add(child.Key, child.Value);

                var childDict = child.Value.GetAllDescendants();

                foreach (var descendant in childDict)
                {
                    if (!dict.ContainsKey(descendant.Key))
                    {
                        dict.Add(descendant.Key, descendant.Value);
                    }
                }
            }

            return dict;
        }
    }

    public class Koder
    {
        public Meta Meta { get; set; }
        public GlobalTreeNode[] Data { get; set; }
    }

    public class Meta
    {
        public string Tittel { get; set; }
        public DateTime Produsert { get; set; }
        public string Utgiver { get; set; }
        public string Url { get; set; }
    }
}