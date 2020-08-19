using System;
using System.Collections.Generic;
using System.Text;

namespace WoWTools.SpellDescParser
{
    public enum PropertyType
    {
        Duration,
        Radius0, // SpellEffect.EffectRadiusIndex[0]
        Radius1, // SpellEffect.EffectRadiusIndex[1]
        Effect,
        Unknown
    }

    public class Property : INode
    {
        readonly PropertyType propertyType;
        readonly uint? index;
        readonly int? overrideSpellID;
        
        public Property(PropertyType propertyType, uint? index, int? spellID)
        {
            this.propertyType = propertyType;
            this.index = index;
            this.overrideSpellID = spellID;
        }

        public override string ToString()
        {
            var returnText = "PROPERTY: Type: " + propertyType;

            if (index != null)
                returnText += ", Index: " + index;
            
            if (overrideSpellID != null)
                returnText += ", Override SpellID: " + overrideSpellID;

            return returnText;
        }

        public void Format(StringBuilder output, int spellID, ISupplier supplier)
        {
            if (overrideSpellID != null)
                spellID = (int)overrideSpellID;

            switch (propertyType)
            {
                case PropertyType.Radius0:
                    var radius0 = supplier.SupplyRadius(spellID, index, 0);
                    output.Append(radius0 == null ? "?" : radius0.ToString());
                    break;
                case PropertyType.Radius1:
                    var radius1 = supplier.SupplyRadius(spellID, index, 1);
                    output.Append(radius1 == null ? "?" : radius1.ToString());
                    break;
                case PropertyType.Duration:
                    var duration = supplier.SupplyDuration(spellID, index);
                    output.Append(duration == null ? "? sec" : duration.ToString() + " sec");
                    break;
                case PropertyType.Effect:
                    // TODO: If effect is negative, is it always made positive? Should this go here or in supplier?
                    var effectPoints = supplier.SupplyEffectPoint(spellID, index);

                    if (effectPoints == null)
                    {
                        output.Append("?");
                    }
                    else
                    {
                        if (effectPoints < 0)
                        {
                            effectPoints *= -1;
                        }

                        output.Append((int)effectPoints);
                    }
    
                    break;
                case PropertyType.Unknown:
                    output.Append("UNKNOWN");
                    break;
                default:
                    output.Append("UNIMPLEMENTED FORMATTING");
                    break;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Property property &&
                   propertyType == property.propertyType &&
                   index == property.index &&
                   overrideSpellID == property.overrideSpellID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(propertyType, index, overrideSpellID);
        }
    }
}
