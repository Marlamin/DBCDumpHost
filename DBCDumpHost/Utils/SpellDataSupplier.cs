using DBCD;
using DBCDumpHost.Services;
using System;
using System.Collections.Generic;
using WoWTools.SpellDescParser;

namespace DBCDumpHost.Utils
{
    public class SpellDataSupplier : ISupplier
    {
        private DBCManager dbcManager;
        private string build;

        public SpellDataSupplier(DBCManager dbcManager, string build)
        {
            this.build = build;
            this.dbcManager = dbcManager;
        }

        public DBCDRow? SupplyEffectRow(int spellID, uint? effectIndex)
        {
            effectIndex ??= 1;

            var spellEffects = dbcManager.FindRecords("SpellEffect", build, "SpellID", spellID).Result;
            if (spellEffects.Count > 0)
            {
                foreach (var spellEffect in spellEffects)
                {
                    if ((int)spellEffect["EffectIndex"] == effectIndex - 1 && (int)spellEffect["DifficultyID"] == 0)
                    {
                        return spellEffect;
                    }
                }
            }

            return null;
        }

        public double? SupplyEffectPoint(int spellID, uint? effectIndex)
        {
            var spellEffect = SupplyEffectRow(spellID, effectIndex);
            return (float?) spellEffect?["EffectBasePointsF"];
        }

        public int? SupplyAuraPeriod(int spellID, uint? effectIndex)
        {
            var spellEffect = SupplyEffectRow(spellID, effectIndex);
            return (int?)spellEffect?["EffectAuraPeriod"];
        }

        public int? SupplyChainTargets(int spellID, uint? effectIndex)
        {
            var spellEffect = SupplyEffectRow(spellID, effectIndex);
            return (int?)spellEffect?["EffectChainTargets"];
        }

        public int? SupplyMaxTargetLevel(int spellID)
        {
            var spellTargetRestrictions = dbcManager.FindRecords("SpellTargetRestrictions", build, "SpellID", spellID, true).Result;
            if (spellTargetRestrictions.Count == 0)
            {
                Console.WriteLine("Unable to find Spell ID " + spellID + " in SpellTargetRestrictions");
                return null;
            }

            return (int)spellTargetRestrictions[0]["MaxTargetLevel"];
        }

        public int? SupplyMaxTargets(int spellID)
        {
            var spellTargetRestrictions = dbcManager.FindRecords("SpellTargetRestrictions", build, "SpellID", spellID, true).Result;
            if (spellTargetRestrictions.Count == 0)
            {
                Console.WriteLine("Unable to find Spell ID " + spellID + " in SpellTargetRestrictions");
                return null;
            }

            return (int)spellTargetRestrictions[0]["MaxTargets"];
        }

        public int? SupplyProcCharges(int spellID)
        {
            var spellAuraOptions = dbcManager.FindRecords("SpellAuraOptions", build, "SpellID", spellID, true).Result;
            if (spellAuraOptions.Count == 0)
            {
                Console.WriteLine("Unable to find Spell ID " + spellID + " in spellAuraOptions");
                return null;
            }

            return (int)spellAuraOptions[0]["ProcCharges"];
        }

        public int? SupplyDuration(int spellID, uint? effectIndex)
        {
            // How is effectIndex used here?
            effectIndex ??= 1;

            var spellMiscRow = dbcManager.FindRecords("spellMisc", build, "SpellID", spellID, true).Result;
            if (spellMiscRow.Count == 0)
            {
                Console.WriteLine("Unable to find Spell ID " + spellID + " in spell misc");
                return null;
            }

            var spellDurationID = (ushort)spellMiscRow[0]["DurationIndex"];
            if (spellDurationID == 0)
            {
                Console.WriteLine("Unable to find duration for Spell ID " + spellID + " index " + effectIndex);
                return null;
            }

            var spellDurationDB = dbcManager.GetOrLoad("SpellDuration", build).Result;
            if (spellDurationDB.TryGetValue(spellDurationID, out var durationRow))
            {
                return (int)durationRow["Duration"];
            }

            Console.WriteLine("Unable to find duration for Spell ID " + spellID + " index " + effectIndex);
            return null;
        }

        public double? SupplyRadius(int spellID, uint? effectIndex, int radiusIndex)
        {
            var spellEffect = SupplyEffectRow(spellID, effectIndex);
            if (spellEffect == null)
                return null;

            var radiusIndexArray = spellEffect.FieldAs<int[]>("EffectRadiusIndex");

            // $a is for first array entry, $A for second
            var spellRadiusID = radiusIndexArray[radiusIndex];

            var spellRadiusDB = dbcManager.GetOrLoad("SpellRadius", build).Result;
            if (spellRadiusDB.TryGetValue(spellRadiusID, out var radiusRow))
            {
                return (float)radiusRow["Radius"];
            }

            Console.WriteLine("Unable to find radius for Spell ID " + spellID + " index " + effectIndex + " radiusIndex " + radiusIndex);
            return null;
        }

        public int? SupplyMaxStacks(int spellID)
        {
            var spellAuraOptions = dbcManager.FindRecords("SpellAuraOptions", build, "SpellID", spellID, true).Result;
            if (spellAuraOptions.Count == 0)
            {
                Console.WriteLine("Unable to find Spell ID " + spellID + " in spellAuraOptions");
                return null;
            }

            return (ushort)spellAuraOptions[0]["CumulativeAura"];
        }
    }
}
