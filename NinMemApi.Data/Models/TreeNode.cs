using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace NinMemApi.Data.Models
{
    public class TreeNode
    {
        public TreeNode()
        {
            Children = new Dictionary<string, TreeNode>();
        }

        public string Code { get; set; }
        public string Name { get; set; }
        public string ParentCode { get; internal set; }
        public string ParentName { get; internal set; }

        public IDictionary<string, TreeNode> Children { get; set; }

        [JsonIgnore]
        public int OwnCount { get; set; }

        public int Count { get { return OwnCount + Children.Values.Sum(c => c.Count); } }

        public TreeNode GetChild(string code)
        {
            return Children.ContainsKey(code) ? Children[code] : null;
        }

        public void AddChild(TreeNode node)
        {
            if (!Children.ContainsKey(node.Code))
            {
                Children.Add(node.Code, node);
            }
        }
    }
}