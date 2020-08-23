using DBCD;
using DBCD.Providers;
using DBCDumpHost.Services;
using DBCDumpHost.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoWTools.SpellDescParser;

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
        public int IconFileDataID { get; set; }
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
            }

            result.IconFileDataID = (int)itemEntry["IconFileDataID"];
            result.ClassID = (byte)itemEntry["ClassID"];
            result.SubClassID = (byte)itemEntry["SubclassID"];
            result.InventoryType = (sbyte)itemEntry["InventoryType"];

            // Icons in Item.db2 can be 0. Look up the proper one in ItemModifiedAppearance => ItemAppearance
            if (result.IconFileDataID == 0)
            {
                var itemModifiedAppearances = await dbcManager.FindRecords("ItemModifiedAppearance", build, "ItemID", itemID);
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
                    result.OverallQualityID = (byte)itemSearchNameEntry["OverallQualityID"];
                }

                result.HasSparse = false;
            }
            else
            {
                result.HasSparse = true;
                result.ItemLevel = (ushort)itemSparseEntry["ItemLevel"];
                result.OverallQualityID = (byte)itemSparseEntry["OverallQualityID"];
                result.Name = (string)itemSparseEntry["Display_lang"];
                result.FlavorText = (string)itemSparseEntry["Description_lang"];
                result.ExpansionID = (byte)itemSparseEntry["ExpansionID"];
                result.RequiredLevel = (sbyte)itemSparseEntry["RequiredLevel"];

                var itemDelay = (ushort)itemSparseEntry["ItemDelay"] / 1000f;
                var targetDamageDB = GetDamageDBByItemSubClass((byte)itemEntry["SubclassID"], (itemSparseEntry.FieldAs<int[]>("Flags")[1] & 0x200) == 0x200);

                var statTypes = itemSparseEntry.FieldAs<sbyte[]>("StatModifier_bonusStat");
                if (statTypes.Length > 0 && statTypes.Any(x => x != -1) && statTypes.Any(x => x != 0))
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

                var damageRecord = await dbcManager.FindRecords(targetDamageDB, build, "ItemLevel", result.ItemLevel);

                var quality = result.OverallQualityID;
                if (quality == 7) // Heirloom == Rare
                    quality = 3;

                if (quality == 5) // Legendary = Epic
                    quality = 4;

                var itemDamage = damageRecord[0].FieldAs<float[]>("Quality")[quality];
                var dmgVariance = (float)itemSparseEntry["DmgVariance"];


                // Use . as decimal separator
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.NumberDecimalSeparator = ".";
                result.MinDamage = Math.Floor(itemDamage * itemDelay * (1 - dmgVariance * 0.5)).ToString(nfi);
                result.MaxDamage = Math.Floor(itemDamage * itemDelay * (1 + dmgVariance * 0.5)).ToString(nfi);
                result.Speed = itemDelay.ToString("F2", nfi);
                result.DPS = itemDamage.ToString("F2", nfi);
            }

            var itemEffectEntries = await dbcManager.FindRecords("ItemEffect", build, "ParentItemID", itemID);
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
        public async Task<IActionResult> GetSpellTooltip(int spellID, string build)
        {
            var result = new TTSpell();
            result.SpellID = spellID;

            var spellNameDB = await dbcManager.GetOrLoad("SpellName", build);
            if (spellNameDB.TryGetValue(spellID, out DBCDRow spellNameRow))
            {
                var spellName = (string)spellNameRow["Name_lang"];
                if (!string.IsNullOrWhiteSpace(spellName))
                {
                    result.Name = spellName;
                }
            }

            var spellDB = await dbcManager.GetOrLoad("Spell", build);
            if (spellDB.TryGetValue(spellID, out var spellRow))
            {
                var dataSupplier = new SpellDataSupplier(dbcManager, build);

                if ((string)spellRow["Description_lang"] != string.Empty)
                {
                    var spellDescParser = new SpellDescParser((string)spellRow["Description_lang"]);
                    spellDescParser.Parse();

                    var sb = new StringBuilder();
                    spellDescParser.root.Format(sb, spellID, dataSupplier);

                    result.Description = sb.ToString();
                }
            }

            var spellMiscRow = dbcManager.FindRecords("spellMisc", build, "SpellID", spellID, true).Result;
            if (spellMiscRow.Count == 0)
            {
                result.IconFileDataID = 134400;
            }
            else
            {
                result.IconFileDataID = (int)spellMiscRow[0]["SpellIconFileDataID"];
            }

            return Ok(result);
        }
    }
}