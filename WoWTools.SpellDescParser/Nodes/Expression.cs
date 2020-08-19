using System;
using System.Collections.Generic;
using System.Text;

namespace WoWTools.SpellDescParser
{
    public class Expression : INode
    {
        string text;

        public Expression(string text)
        {
            this.text = text;
        }

        public override bool Equals(object obj)
        {
            return obj is Expression text &&
                   this.text == text.text;
        }
        public override string ToString()
        {
            return "EXPRESSION: " + text;
        }

        public void Format(StringBuilder output, int spellID, ISupplier supplier)
        {
            output.Append("{" + text + "}");
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(text);
        }
    }
}
