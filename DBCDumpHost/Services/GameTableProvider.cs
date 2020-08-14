using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DBCDumpHost.Services
{
    public static class GameTableProvider
    {
        private static Dictionary<string, Dictionary<int, CombatRatingsMultByILVLRow>> combatRatingMultiByILVL;

        public struct CombatRatingsMultByILVLRow
        {
            public double ArmorMultiplier;
            public double WeaponMultiplier;
            public double TrinketMultiplier;
            public double JewelryMultiplier;
        }

        static GameTableProvider()
        {
            combatRatingMultiByILVL = new Dictionary<string, Dictionary<int, CombatRatingsMultByILVLRow>>();
        }

        public static CombatRatingsMultByILVLRow GetCombatRatingsMultByILVLRow(int itemLevel, string build)
        {
            if(!combatRatingMultiByILVL.ContainsKey(build))
            {
                using (var client = new HttpClient())
                {
                    var tempDict = new Dictionary<int, CombatRatingsMultByILVLRow>();
                    var output = client.GetStringAsync(SettingManager.cascToolHost + "/casc/file/gametable?gameTableName=CombatRatingsMultByILvl&fullBuild=" + build).Result;
                    var lines = output.Split("\r\n");
                    for(var i = 1; i < lines.Length; i++)
                    {
                        if (lines[i].Length == 0)
                            continue;

                        var fields = lines[i].Split('\t');
                        tempDict.Add(int.Parse(fields[0]), 
                            new CombatRatingsMultByILVLRow() { 
                                ArmorMultiplier = double.Parse(fields[1], CultureInfo.InvariantCulture), 
                                WeaponMultiplier = double.Parse(fields[2], CultureInfo.InvariantCulture), 
                                TrinketMultiplier = double.Parse(fields[3], CultureInfo.InvariantCulture), 
                                JewelryMultiplier = double.Parse(fields[4], CultureInfo.InvariantCulture) 
                            });
                    }
                    combatRatingMultiByILVL.Add(build, tempDict);
                }
            }

            if (combatRatingMultiByILVL.TryGetValue(build, out var buildDict))
            {
                if (buildDict.TryGetValue(itemLevel, out var row))
                {
                    return row;
                }
                else
                {
                    throw new Exception("Target itemLevel not found in gametable!");
                }
            }
            else
            {
                throw new Exception("Target build not found in gametable cache!");
            }
        }

    }
}
