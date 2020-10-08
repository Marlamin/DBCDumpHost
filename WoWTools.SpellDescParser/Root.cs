using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WoWTools.SpellDescParser
{
    public class Root : INode
    {
        public List<INode> nodes;

        public Root(List<INode> nodes)
        {
            this.nodes = nodes;
        }

        public void AddNode(INode node)
        {
            nodes.Add(node);
        }

        public override bool Equals(object obj)
        {
            return obj is Root root && this.nodes.SequenceEqual(root.nodes);
        }

        public override string ToString()
        {
            return string.Join('\n', nodes);
        }

        public void Format(StringBuilder output, int spellID, ISupplier supplier)
        {
            foreach (var node in nodes)
            {
                node.Format(output, spellID, supplier);
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(nodes);
        }
    }
}
