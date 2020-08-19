using DBCDumpHost.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public double? SupplyEffectPoint(int spellID, uint? effectIndex)
        {
            if (effectIndex == null)
                effectIndex = 1;

            var spellEffects = dbcManager.FindRecords("SpellEffect", build, "SpellID", spellID).Result;
            if (spellEffects.Count > 0)
            {
                var orderedEffects = spellEffects.ToDictionary(x => (int)x["EffectIndex"], x => (double)(Single)x["EffectBasePointsF"]);

                if (orderedEffects.TryGetValue((int)effectIndex - 1, out var basePointsF))
                {
                    return basePointsF;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                // Find a better way to deal with not found effects
                return null;
            }
        }

        public int? SupplyDuration(int spellID, uint? effectIndex)
        {
            if (effectIndex == null)
                effectIndex = 1;

            var spellDurationID = 0;

            var spellMiscRow = dbcManager.FindRecords("spellMisc", build, "SpellID", spellID, true).Result;
            if (spellMiscRow.Count == 0)
            {
                Console.WriteLine("Unable to find Spell ID " + spellID + " in spell misc");
                return null;
            }

            spellDurationID = (ushort)spellMiscRow[0]["DurationIndex"];

            if (spellDurationID == 0)
            {
                Console.WriteLine("Unable to find duration for Spell ID " + spellID + " index " + effectIndex);
                return null;
            }

            var spellDurationDB = dbcManager.GetOrLoad("SpellDuration", build).Result;
            if (spellDurationDB.TryGetValue(spellDurationID, out var durationRow))
            {
                return (int)durationRow["Duration"] / 1000;
            }
            else
            {
                Console.WriteLine("Unable to find duration for Spell ID " + spellID + " index " + effectIndex);
                return null;
            }
        }

        public double? SupplyRadius(int spellID, uint? effectIndex, int radiusIndex)
        {
            if (effectIndex == null)
                effectIndex = 1;

            var spellRadiusID = 0;

            var spellEffects = dbcManager.FindRecords("SpellEffect", build, "SpellID", spellID).Result;
            foreach (var spellEffect in spellEffects)
            {
                if ((int)spellEffect["EffectIndex"] == effectIndex - 1)
                {
                    var radiusIndexArray = spellEffect.FieldAs<int[]>("EffectRadiusIndex");

                    // $a is for first array entry, $A for second
                    spellRadiusID = radiusIndexArray[radiusIndex];
                }
            }

            var spellRadiusDB = dbcManager.GetOrLoad("SpellRadius", build).Result;
            if (spellRadiusDB.TryGetValue(radiusIndex, out var radiusRow))
            {
                return (double)(Single)radiusRow["Radius"];
            }
            else
            {
                Console.WriteLine("Unable to find radius for Spell ID " + spellID + " index " + effectIndex + " radiusIndex " + radiusIndex);
                return null;
            }
        }
    }
}
