using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DBCD;
using DBCD.Providers;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DBCDumpHost.Controllers
{
    public struct TTItem
    {
        public string Name { get; set; }
        public int IconFileDataID { get; set; }
        public byte ExpansionID { get; set; }
        public byte ClassID { get; set; }
        public byte SubClassID { get; set; }
        public sbyte InventoryType { get; set; }
        public ushort ItemLevel { get; set; }
        public byte OverallQualityID { get; set; }
        public bool HasSparse { get; set; }
        public string FlavorText { get; set; }
        public TTItemEffect[] ItemEffects { get; set; }
        public TTItemStat[] Stats { get; set; }
        public string Speed { get; set; }
        public string DPS { get; set; }
        public string MinDamage { get; set; }
        public string MaxDamage { get; set; }
        public sbyte RequiredLevel { get; set; }
    }

    public struct TTItemEffect
    {
        public TTSpell Spell { get; set; }
        public sbyte TriggerType { get; set; }
    }

    public struct TTSpell
    {
        public int SpellID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public struct TTItemStat
    {
        public sbyte StatTypeID { get; set; }
        public int Value { get; set; }
        public bool IsCombatRating { get; set; }
    }

    [Route("api/tooltip")]
    [ApiController]
    public class TooltipController : ControllerBase
    {
        private readonly DBDProvider dbdProvider;
        private readonly DBCManager dbcManager;

        public TooltipController(IDBDProvider dbdProvider, IDBCManager dbcManager)
        {
            this.dbdProvider = dbdProvider as DBDProvider;
            this.dbcManager = dbcManager as DBCManager;
        }

        // TEMP -- Testing tooltips for incomplete item data
        [HttpGet("unkItems")]
        public async Task<IActionResult> GetUnkItems()
        {
            var build = "9.0.1.35522";
            var itemDB = await dbcManager.GetOrLoad("Item", build);
            var itemSparseDB = await dbcManager.GetOrLoad("ItemSparse", build, true);
            var itemSearchNameDB = await dbcManager.GetOrLoad("ItemSearchName", build, true);
            var unkItems = new List<int>();
            foreach (var itemID in itemDB.Keys)
            {
                if (!itemSparseDB.ContainsKey(itemID) && !itemSearchNameDB.ContainsKey(itemID))
                {
                    unkItems.Add(itemID);
                }
            }
            return Ok(unkItems);
        }

        [HttpGet("item/{ItemID}")]
        public async Task<IActionResult> GetItemTooltip(int itemID, string build)
        {
            var result = new TTItem();

            var itemDB = await dbcManager.GetOrLoad("Item", build);
            if (!itemDB.TryGetValue(itemID, out DBCDRow itemEntry))
            {
                return NotFound();
                throw new KeyNotFoundException("Unable to find ID " + itemID + " in Item.db2");
            }

            result.IconFileDataID = (int)itemEntry["IconFileDataID"];
            result.ClassID = (byte)itemEntry["ClassID"];
            result.SubClassID = (byte)itemEntry["SubclassID"];
            result.InventoryType = (sbyte)itemEntry["InventoryType"];

            if (result.IconFileDataID == 0)
            {
                // Look in ItemModifiedAppearance => ItemAppearance for proper icon
                var itemModifiedAppearances = await FindRecords("ItemModifiedAppearance", build, "ItemID", itemID);
                if (itemModifiedAppearances.Count > 0)
                {
                    var itemAppearanceDB = await dbcManager.GetOrLoad("ItemAppearance", build);
                    if (itemAppearanceDB.TryGetValue((ushort)itemModifiedAppearances[0]["ItemAppearanceID"], out DBCDRow itemAppearanceRow))
                    {
                        result.IconFileDataID = (int)itemAppearanceRow["DefaultIconFileDataID"];
                    }
                }
            }

            var itemSparseDB = await dbcManager.GetOrLoad("ItemSparse", build);
            if (!itemSparseDB.TryGetValue(itemID, out DBCDRow itemSparseEntry))
            {
                var itemSearchNameDB = await dbcManager.GetOrLoad("ItemSearchName", build);
                if (!itemSearchNameDB.TryGetValue(itemID, out DBCDRow itemSearchNameEntry))
                {
                    result.Name = "Unknown Item";
                }
                else
                {
                    result.Name = (string)itemSearchNameEntry["Display_lang"];
                    result.RequiredLevel = (sbyte)itemSearchNameEntry["RequiredLevel"];
                    result.ExpansionID = (byte)itemSearchNameEntry["ExpansionID"];
                    result.ItemLevel = (ushort)itemSearchNameEntry["ItemLevel"];
                }

                result.HasSparse = false;
            }
            else
            {
                var itemDelay = (ushort)itemSparseEntry["ItemDelay"] / 1000f;
                var targetDamageDB = GetDamageDBByItemSubClass((byte)itemEntry["SubclassID"], (itemSparseEntry.FieldAs<int[]>("Flags")[1] & 0x200) == 0x200);

                // Use . as decimal separator
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.NumberDecimalSeparator = ".";

                result.ItemLevel = (ushort)itemSparseEntry["ItemLevel"];
                result.OverallQualityID = (byte)itemSparseEntry["OverallQualityID"];

                var statTypes = itemSparseEntry.FieldAs<sbyte[]>("StatModifier_bonusStat");
                if (statTypes.Length > 0 && !statTypes.All(x => x == -1) && !statTypes.All(x => x == 0))
                {
                    var (RandomPropField, RandomPropIndex) = TooltipUtils.GetRandomPropertyByInventoryType(result.OverallQualityID, result.InventoryType, result.SubClassID, build);

                    var randomPropDB = await dbcManager.GetOrLoad("RandPropPoints", build);
                    int randProp;
                    if (randomPropDB.TryGetValue(result.ItemLevel, out DBCDRow randPropEntry))
                    {
                        randProp = (int)randPropEntry.FieldAs<uint[]>(RandomPropField)[RandomPropIndex];
                    }
                    else
                    {
                        throw new Exception("Item Level " + result.ItemLevel + " not found in RandPropPoints");
                    }

                    var statPercentEditor = itemSparseEntry.FieldAs<int[]>("StatPercentEditor");

                    var statList = new Dictionary<sbyte, TTItemStat>();
                    for (var statIndex = 0; statIndex < statTypes.Length; statIndex++)
                    {
                        if (statTypes[statIndex] == -1 || statTypes[statIndex] == 0)
                            continue;

                        var stat = TooltipUtils.CalculateItemStat(statTypes[statIndex], randProp, result.ItemLevel, statPercentEditor[statIndex], 0.0f, result.OverallQualityID, result.InventoryType, result.SubClassID, build);

                        if (stat.Value == 0)
                            continue;

                        if (statList.TryGetValue(statTypes[statIndex], out var currStat))
                        {
                            currStat.Value += stat.Value;
                        }
                        else
                        {
                            statList.Add(statTypes[statIndex], stat);
                        }
                    }

                    result.Stats = statList.Values.ToArray();
                }

                var damageRecord = await FindRecords(targetDamageDB, build, "ItemLevel", result.ItemLevel);
                var itemDamage = damageRecord[0].FieldAs<float[]>("Quality")[result.OverallQualityID];
                var dmgVariance = (float)itemSparseEntry["DmgVariance"];

                result.HasSparse = true;
                result.Name = (string)itemSparseEntry["Display_lang"];
                result.FlavorText = (string)itemSparseEntry["Description_lang"];
                result.ExpansionID = (byte)itemSparseEntry["ExpansionID"];
                result.RequiredLevel = (sbyte)itemSparseEntry["RequiredLevel"];

                result.Speed = itemDelay.ToString("F2", nfi);
                result.DPS = itemDamage.ToString("F2", nfi);
                result.MinDamage = Math.Floor(itemDamage * itemDelay * (1 - dmgVariance * 0.5)).ToString();
                result.MaxDamage = Math.Floor(itemDamage * itemDelay * (1 + dmgVariance * 0.5)).ToString();
            }

            var itemEffectEntries = await FindRecords("ItemEffect", build, "ParentItemID", itemID);
            if (itemEffectEntries.Count > 0)
            {
                var spellDB = await dbcManager.GetOrLoad("Spell", build);
                var spellNameDB = await dbcManager.GetOrLoad("SpellName", build);

                result.ItemEffects = new TTItemEffect[itemEffectEntries.Count];
                for (var i = 0; i < itemEffectEntries.Count; i++)
                {
                    result.ItemEffects[i].TriggerType = (sbyte)itemEffectEntries[i]["TriggerType"];

                    var ttSpell = new TTSpell { SpellID = (int)itemEffectEntries[i]["SpellID"] };
                    if (spellDB.TryGetValue((int)itemEffectEntries[i]["SpellID"], out DBCDRow spellRow))
                    {
                        var spellDescription = (string)spellRow["Description_lang"];
                        if (!string.IsNullOrWhiteSpace(spellDescription))
                        {
                            ttSpell.Description = spellDescription;
                        }
                    }

                    if (spellNameDB.TryGetValue((int)itemEffectEntries[i]["SpellID"], out DBCDRow spellNameRow))
                    {
                        var spellName = (string)spellNameRow["Name_lang"];
                        if (!string.IsNullOrWhiteSpace(spellName))
                        {
                            ttSpell.Name = spellName;
                        }
                    }

                    result.ItemEffects[i].Spell = ttSpell;
                }
            }

            /* Fixups */
            // Classic ExpansionID column has 254, make 0. ¯\_(ツ)_/¯
            if (result.ExpansionID == 254)
                result.ExpansionID = 0;

            return Ok(result);
        }

        private string GetDamageDBByItemSubClass(byte itemSubClassID, bool isCasterWeapon)
        {
            switch (itemSubClassID)
            {
                // 1H
                case 0:  //	Axe
                case 4:  //	Mace
                case 7:  //	Sword
                case 9:  //	Warglaives
                case 11: //	Bear Claws
                case 13: //	Fist Weapon
                case 15: //	Dagger
                case 16: //	Thrown
                case 19: //	Wand,
                    if (isCasterWeapon)
                    {
                        return "ItemDamageOneHandCaster";
                    }
                    else
                    {
                        return "ItemDamageOneHand";
                    }
                // 2H
                case 1:  // 2H Axe
                case 2:  // Bow
                case 3:  // Gun
                case 5:  // 2H Mace
                case 6:  // Polearm
                case 8:  // 2H Sword
                case 10: //	Staff,
                case 12: //	Cat Claws,
                case 17: //	Spear,
                case 18: //	Crossbow
                case 20: //	Fishing Pole
                    if (isCasterWeapon)
                    {
                        return "ItemDamageTwoHandCaster";
                    }
                    else
                    {
                        return "ItemDamageTwoHand";
                    }
                case 14: //	14: 'Miscellaneous',
                    return "ItemDamageOneHandCaster";
                default:
                    throw new Exception("Don't know what table to map to unknown SubClassID " + itemSubClassID);
            }
        }


        [HttpGet("spell/{SpellID}")]
        public string GetSpellTooltip(int spellID)
        {
            return "Spell tooltip for " + spellID;
        }

        private async Task<List<DBCDRow>> FindRecords(string name, string build, string col, int val)
        {
            var rowList = new List<DBCDRow>();

            var storage = await dbcManager.GetOrLoad(name, build);
            if (col == "ID")
            {
                if (storage.TryGetValue(val, out DBCDRow row))
                {
                    for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                    {
                        string fieldName = storage.AvailableColumns[i];

                        if (fieldName != col)
                            continue;

                        // Don't think FKs to arrays are possible, so only check regular value
                        if (row[fieldName].ToString() == val.ToString())
                            rowList.Add(row);
                    }
                }
            }
            else
            {
                foreach (DBCDRow row in storage.Values)
                {
                    for (var i = 0; i < storage.AvailableColumns.Length; ++i)
                    {
                        string fieldName = storage.AvailableColumns[i];

                        if (fieldName != col)
                            continue;

                        // Don't think FKs to arrays are possible, so only check regular value
                        if (row[fieldName].ToString() == val.ToString())
                            rowList.Add(row);
                    }
                }
            }

            return rowList;
        }
    }
}