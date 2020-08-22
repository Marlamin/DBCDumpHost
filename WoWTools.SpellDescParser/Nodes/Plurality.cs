using System;
using System.Collections.Generic;
using System.Text;

namespace WoWTools.SpellDescParser
{
    public class Plurality : INode
    {
        private string singular;
        private string plural;

        public Plurality(string singular, string plural)
        {
            this.singular = singular;
            this.plural = plural;
        }

        public override bool Equals(object obj)
        {
            return obj is Plurality plurality &&
                   this.singular == plurality.singular && this.plural == plurality.plural;
        }
        public override string ToString()
        {
            return "PLURALITY: Singular: " + singular + ", Plural: " + plural;
        }

        public void Format(StringBuilder output, int spellID, ISupplier supplier)
        {
            // Somehow get previous value... :thinking:
            //output.Append(text);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(singular, plural);
        }
    }
}
