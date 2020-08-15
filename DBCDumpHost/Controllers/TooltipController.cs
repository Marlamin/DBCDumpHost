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
    struct TTItem
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
        public TTItemEffect[] ItemEffects { get; set; }
        public TTItemStat[] Stats { get; set; }
        public string Speed { get; set; }
        public string DPS { get; set; }
        public string MinDamage { get; set; }
        public string MaxDamage { get; set; }
        public sbyte RequiredLevel { get; set; }
    }

    struct TTItemEffect
    {
        public TTSpell Spell { get; set; } 
        public sbyte TriggerType { get; set; }
    }

    struct TTSpell
    {
        public int SpellID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    struct TTItemStat
    {
        public sbyte StatTypeID { get; set; }
        public double Value { get; set; }
    }

    enum InventoryType : sbyte
    {
        NonEquippable = 0,
        Head = 1,
        Neck = 2,
        Shoulder = 3,
        Shirt = 4,
        Chest = 5,
        Waist = 6,
        Legs = 7,
        Feet = 8,
        Wrist = 9,
        Hands = 10,
        Finger = 11,
        Trinket = 12,
        OneHand = 13,
        Shield = 14,
        Ranged = 15,
        Back = 16,
        TwoHand = 17,
        Bag = 18,
        Tabard = 19,
        Robe = 20,
        MainHand = 21,
        OffHand = 22,
        HeldInOffhand = 23,
        Ammo = 24,
        Thrown = 25,
        RangedRight = 26,
        Quiver = 27,
        Relic = 28
    }

    enum ItemStatType : sbyte
    {
        MANA = 0,
        HEALTH = 1,
        AGILITY = 3,
        STRENGTH = 4,
        INTELLECT = 5,
        SPIRIT = 6,
        STAMINA = 7,
        DEFENSE_SKILL_RATING = 12,
        DODGE_RATING = 13,
        PARRY_RATING = 14,
        BLOCK_RATING = 15,
        HIT_MELEE_RATING = 16,
        HIT_RANGED_RATING = 17,
        HIT_SPELL_RATING = 18,
        CRIT_MELEE_RATING = 19,
        CRIT_RANGED_RATING = 20,
        CRIT_SPELL_RATING = 21,
        CORRUPTION = 22,
        CORRUPTION_RESISTANCE = 23,
        MODIFIED_CRAFTING_STAT_1 = 24,
        MODIFIED_CRAFTING_STAT_2 = 25,
        CRIT_TAKEN_RANGED_RATING = 26,
        CRIT_TAKEN_SPELL_RATING = 27,
        HASTE_MELEE_RATING = 28,
        HASTE_RANGED_RATING = 29,
        HASTE_SPELL_RATING = 30,
        HIT_RATING = 31,
        CRIT_RATING = 32,
        HIT_TAKEN_RATING = 33,
        CRIT_TAKEN_RATING = 34,
        RESILIENCE_RATING = 35,
        HASTE_RATING = 36,
        EXPERTISE_RATING = 37,
        ATTACK_POWER = 38,
        RANGED_ATTACK_POWER = 39,
        VERSATILITY = 40,
        SPELL_HEALING_DONE = 41,
        SPELL_DAMAGE_DONE = 42,
        MANA_REGENERATION = 43,
        ARMOR_PENETRATION_RATING = 44,
        SPELL_POWER = 45,
        HEALTH_REGEN = 46,
        SPELL_PENETRATION = 47,
        BLOCK_VALUE = 48,
        MASTERY_RATING = 49,
        EXTRA_ARMOR = 50,
        FIRE_RESISTANCE = 51,
        FROST_RESISTANCE = 52,
        HOLY_RESISTANCE = 53,
        SHADOW_RESISTANCE = 54,
        NATURE_RESISTANCE = 55,
        ARCANE_RESISTANCE = 56,
        PVP_POWER = 57,
        CR_AMPLIFY = 58,
        CR_MULTISTRIKE = 59,
        CR_READINESS = 60,
        CR_SPEED = 61,
        CR_LIFESTEAL = 62,
        CR_AVOIDANCE = 63,
        CR_STURDINESS = 64
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
            foreach(var itemID in itemDB.Keys)
            {
                if(!itemSparseDB.ContainsKey(itemID) && !itemSearchNameDB.ContainsKey(itemID))
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

            if(result.IconFileDataID == 0)
            {
                // Look in ItemModifiedAppearance => ItemAppearance for proper icon
                var itemModifiedAppearances = await FindRecords("ItemModifiedAppearance", build, "ItemID", itemID);
                if(itemModifiedAppearances.Count > 0)
                {
                    var itemAppearanceDB = await dbcManager.GetOrLoad("ItemAppearance", build);
                    if(itemAppearanceDB.TryGetValue((ushort)itemModifiedAppearances[0]["ItemAppearanceID"], out DBCDRow itemAppearanceRow)){
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
                if(statTypes.Length > 0 && !statTypes.All(x => x == -1) && !statTypes.All(x => x == 0))
                {
                    var randProp = await GetRandomPropertyByInventoryType(result.ItemLevel, result.OverallQualityID, (InventoryType)result.InventoryType, result.SubClassID, build);
                    
                    var statPercentEditor = itemSparseEntry.FieldAs<int[]>("StatPercentEditor");

                    var statList = new Dictionary<sbyte, TTItemStat>();
                    for (var statIndex = 0; statIndex < statTypes.Length; statIndex++)
                    {
                        if (statTypes[statIndex] == -1 || statTypes[statIndex] == 0)
                            continue;


                        var multiplierRow = new GameTableProvider.MultByILVLRow(){
                            ArmorMultiplier = 1.0d,
                            JewelryMultiplier = 1.0d,
                            TrinketMultiplier = 1.0d,
                            WeaponMultiplier = 1.0d
                        };

                        if (IsCombatRating(statTypes[statIndex]))
                        {
                            multiplierRow = GameTableProvider.GetCombatRatingsMultByILVLRow(result.ItemLevel, build);
                        }
                        else if((ItemStatType)statTypes[statIndex] == ItemStatType.STAMINA)
                        {
                            multiplierRow = GameTableProvider.GetStaminaMultByILVLRow(result.ItemLevel, build);
                        }


                        double calculatedValue;

                        if ((InventoryType)result.InventoryType == InventoryType.Neck || (InventoryType)result.InventoryType == InventoryType.Finger)
                        {
                            calculatedValue = randProp * statPercentEditor[statIndex] * multiplierRow.JewelryMultiplier * 0.0001;
                        }
                        else
                        {
                            calculatedValue = randProp * statPercentEditor[statIndex] * multiplierRow.ArmorMultiplier * 0.0001;
                        }

                        if (statList.TryGetValue(statTypes[statIndex], out var currStat))
                        {
                            currStat.Value += calculatedValue;
                        }
                        else
                        {
                            statList.Add(statTypes[statIndex], new TTItemStat()
                            {
                                StatTypeID = statTypes[statIndex],
                                Value = calculatedValue
                            });
                        }
                    }

                    result.Stats = statList.Values.ToArray();
                }

                var damageRecord = await FindRecords(targetDamageDB, build, "ItemLevel", result.ItemLevel);
                var itemDamage = damageRecord[0].FieldAs<float[]>("Quality")[result.OverallQualityID];
                var dmgVariance = (float)itemSparseEntry["DmgVariance"];

                result.HasSparse = true;
                result.Name = (string)itemSparseEntry["Display_lang"];
                result.ExpansionID = (byte)itemSparseEntry["ExpansionID"];
                result.RequiredLevel = (sbyte)itemSparseEntry["RequiredLevel"];

                result.Speed = itemDelay.ToString("F2", nfi);
                result.DPS = itemDamage.ToString("F2", nfi);
                result.MinDamage = Math.Floor(itemDamage * itemDelay * (1 - dmgVariance * 0.5)).ToString();
                result.MaxDamage = Math.Floor(itemDamage * itemDelay * (1 + dmgVariance * 0.5)).ToString();
            }

            var itemEffectEntries = await FindRecords("ItemEffect", build, "ParentItemID", itemID);
            if(itemEffectEntries.Count > 0)
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

                    if(spellNameDB.TryGetValue((int)itemEffectEntries[i]["SpellID"], out DBCDRow spellNameRow))
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

        private bool IsCombatRating(sbyte StatTypeID)
        {
            switch ((ItemStatType)StatTypeID)
            {
                case ItemStatType.MASTERY_RATING:
                case ItemStatType.DODGE_RATING:
                case ItemStatType.PARRY_RATING:
                case ItemStatType.BLOCK_RATING:
                case ItemStatType.HIT_MELEE_RATING:
                case ItemStatType.HIT_RANGED_RATING:
                case ItemStatType.HIT_SPELL_RATING:
                case ItemStatType.CRIT_MELEE_RATING:
                case ItemStatType.CRIT_RANGED_RATING:
                case ItemStatType.CRIT_SPELL_RATING:
                case ItemStatType.CRIT_TAKEN_RANGED_RATING:
                case ItemStatType.CRIT_TAKEN_SPELL_RATING:
                case ItemStatType.HASTE_MELEE_RATING:
                case ItemStatType.HASTE_RANGED_RATING:
                case ItemStatType.HASTE_SPELL_RATING:
                case ItemStatType.HIT_RATING:
                case ItemStatType.CRIT_RATING:
                case ItemStatType.HIT_TAKEN_RATING:
                case ItemStatType.CRIT_TAKEN_RATING:
                case ItemStatType.RESILIENCE_RATING:
                case ItemStatType.HASTE_RATING:
                case ItemStatType.EXPERTISE_RATING:
                case ItemStatType.CR_MULTISTRIKE:
                case ItemStatType.CR_SPEED:
                case ItemStatType.CR_LIFESTEAL:
                case ItemStatType.CR_AVOIDANCE:
                case ItemStatType.VERSATILITY:
                case ItemStatType.EXTRA_ARMOR:
                    return true;
                default:
                    return false;
            }
        }

        private async Task<int> GetRandomPropertyByInventoryType(ushort itemLevel, byte overallQualityID, InventoryType inventoryType, byte subClassID, string build)
        {
            if (overallQualityID == 0)
                return 0;

            sbyte targetIndex = -1;
            switch (inventoryType)
            {
                case InventoryType.Head:
                case InventoryType.Shirt:
                case InventoryType.Chest:
                case InventoryType.Legs:
                case InventoryType.Ranged:
                case InventoryType.TwoHand:
                case InventoryType.Robe:
                case InventoryType.Thrown:
                    targetIndex = 0;
                    break;
                case InventoryType.Shoulder:
                case InventoryType.Waist:
                case InventoryType.Feet:
                case InventoryType.Hands:
                case InventoryType.Trinket:
                    targetIndex = 1;
                    break;
                case InventoryType.Neck:
                case InventoryType.Wrist:
                case InventoryType.Finger:
                case InventoryType.Back:
                    targetIndex = 2;
                    break;
                case InventoryType.OneHand:
                case InventoryType.Shield:
                case InventoryType.MainHand:
                case InventoryType.OffHand:
                case InventoryType.HeldInOffhand:
                    targetIndex = 3;
                    break;
                case InventoryType.RangedRight:
                    targetIndex = 3;
                    if(subClassID != 19) // Wands
                        targetIndex = 0;
                    break;
                case InventoryType.Relic:
                    targetIndex = 4;
                    break;
            }
                    
            var randomPropDB = await dbcManager.GetOrLoad("RandPropPoints", build);
            if (randomPropDB.TryGetValue(itemLevel, out DBCDRow randPropEntry))
            {
                string targetField = overallQualityID switch
                {
                    2 => "Good",
                    3 => "Superior",
                    4 => "Epic",
                    _ => throw new Exception("Unsupported quality: " + overallQualityID),
                };
                return (int)randPropEntry.FieldAs<uint[]>(targetField)[targetIndex];
            }
            else
            {
                throw new Exception("Item Level " + itemLevel + " not found in RandPropPoints");
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