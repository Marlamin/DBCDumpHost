using System.Collections.Generic;
using System.Text;

namespace WoWTools.SpellDescParser
{
    public interface INode
    {
        void Format(StringBuilder output, int spellID, ISupplier supplier);
    }
}
