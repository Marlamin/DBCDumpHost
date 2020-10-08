using System;
using System.Collections.Generic;
using System.Text;

namespace WoWTools.SpellDescParser
{
    public enum PropertyType
    {
        Effect,
        EffectAmplitude,        // SpellEffect.EffectAmplitude
        Duration,
        Radius0,                // SpellEffect.EffectRadiusIndex[0]
        Radius1,                // SpellEffect.EffectRadiusIndex[1]
        MaxStacks,              // SpellAuraOptions.CumulativeAura
        AuraPeriod,             // SpellEffect.AuraPeriod
        ProcCharges,            // SpellAuraOptions.ProcCharges
        ProcChance,             // SpellAuraOptions.ProcChance
        ChainTargets,           // SpellEffect.ChainTargets
        MaxTargetLevel,         // SpellTargetRestrictions.MaxTargetLevel
        MaxTargets,             // SpellTargetRestrictions.MaxTargets
        MinRange,               // SpellRange::ID
        MaxRange,               // SpellRange::ID
        HearthstoneLocation,
        SpellName,
        SpellDescription,
        Unknown
    }

    public class Property : INode
    {
        public readonly PropertyType propertyType;
        readonly uint? index;
        public readonly int? overrideSpellID;
        
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
                    // TODO: Proper time parsing?
                    var duration = supplier.SupplyDuration(spellID, index);
                    if (duration == null)
                    {
                        output.Append("?");
                    }
                    else
                    {
                        duration = duration / 1000;
                        if (duration == 3600)
                        {
                            output.Append("1 hour");
                        }
                        else
                        {
                            output.Append(duration == null ? "? sec" : duration.ToString() + " sec");
                        }
                    }
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
                case PropertyType.MaxStacks:
                    var maxStacks = supplier.SupplyMaxStacks(spellID);
                    output.Append(maxStacks == null ? "?" : maxStacks.ToString());
                    break;
                case PropertyType.AuraPeriod:
                    var auraPeriod = supplier.SupplyAuraPeriod(spellID, index);
                    if (auraPeriod == null)
                    {
                        output.Append("?");
                    }
                    else
                    {
                        auraPeriod = auraPeriod / 1000;
                        if (auraPeriod == 3600)
                        {
                            output.Append("1 hour");
                        }
                        else
                        {
                            output.Append(auraPeriod == null ? "?" : auraPeriod.ToString());
                        }
                    }
                    break;
                case PropertyType.ProcCharges:
                    var procCharges = supplier.SupplyProcCharges(spellID);
                    output.Append(procCharges == null ? "?" : procCharges.ToString());
                    break;
                case PropertyType.ProcChance:
                    var procChance = supplier.SupplyProcChance(spellID);
                    output.Append(procChance == null ? "?" : procChance.ToString());
                    break;
                case PropertyType.ChainTargets:
                    var chainTargets = supplier.SupplyChainTargets(spellID, index);
                    output.Append(chainTargets == null ? "?" : chainTargets.ToString());
                    break;
                case PropertyType.MaxTargetLevel:
                    var maxTargetLevel = supplier.SupplyMaxTargetLevel(spellID);
                    output.Append(maxTargetLevel == null ? "?" : maxTargetLevel.ToString());
                    break;
                case PropertyType.MaxTargets:
                    var maxTargets = supplier.SupplyMaxTargets(spellID);
                    output.Append(maxTargets == null ? "?" : maxTargets.ToString());
                    break;
                case PropertyType.MinRange:
                    var minRange = supplier.SupplyMinRange(spellID);
                    output.Append(minRange == null ? "?" : minRange.ToString());
                    break;
                case PropertyType.MaxRange:
                    var maxRange = supplier.SupplyMaxRange(spellID);
                    output.Append(maxRange == null ? "?" : maxRange.ToString());
                    break;
                case PropertyType.EffectAmplitude:
                    var effectAmplitude = supplier.SupplyEffectAmplitude(spellID, index);
                    output.Append(effectAmplitude == null ? "?" : effectAmplitude.ToString());
                    break;
                case PropertyType.SpellName:
                    var spellName = supplier.SupplySpellName(spellID);
                    output.Append(spellName == null ? "UNKNOWN SPELL" : spellName);
                    break;
                case PropertyType.HearthstoneLocation:
                    output.Append("your Hearthstone location");
                    break;
                case PropertyType.SpellDescription:
                    output.Append("$@spelldesc" + spellID);
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
