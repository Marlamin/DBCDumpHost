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
        public string name { get; set; }
        public int iconFileDataID { get; set; }
        public byte expansionID { get; set; }
        public ushort itemLevel { get; set; }
        public byte overallQualityID { get; set; } 
        public bool hasSparse { get; set; }
        public TTItemEffect[] itemEffects { get; set; }
        public string speed { get; set; }
        public string dps { get; set; }
        public string minDamage { get; set; }
        public string maxDamage { get; set; }
    }

    struct TTItemEffect
    {
        public TTSpell spell { get; set; } 
        public sbyte triggerType { get; set; }
    }

    struct TTSpell
    {
        public int spellID { get; set; }
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

        [HttpGet("item/{ItemID}")]
        public async Task<IActionResult> GetItemTooltip(int itemID, string build)
        {
            var result = new TTItem();

            var itemDB = await dbcManager.GetOrLoad("Item", build);
            if(!itemDB.TryGetValue(itemID, out DBCDRow itemEntry))
            {
                return NotFound();
                throw new KeyNotFoundException("Unable to find ID " + itemID + " in Item.db2");
            }

            // TODO: Look in ItemModifiedAppearance => ItemAppearance for proper icon if icon in Item is 0.
            result.iconFileDataID = (int)itemEntry["IconFileDataID"];

            var itemSparseDB = await dbcManager.GetOrLoad("ItemSparse", build);
            if (!itemSparseDB.TryGetValue(itemID, out DBCDRow itemSparseEntry))
            {
                var itemSearchNameDB = await dbcManager.GetOrLoad("ItemSearchName", build);
                if (!itemSearchNameDB.TryGetValue(itemID, out DBCDRow itemSearchNameEntry))
                {
                    result.name = "Unknown Item";
                }
                else
                {
                    result.name = (string)itemSearchNameEntry["Display_lang"];
                }

                result.hasSparse = false;
            }
            else
            {
                var itemDelay = (ushort)itemSparseEntry["ItemDelay"] / 1000f;

                var isCasterWeapon = (itemSparseEntry.FieldAs<int[]>("Flags")[1] & 0x200) == 0x200;

                var targetDamageDB = "";

                switch ((byte)itemEntry["SubclassID"])
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
                            targetDamageDB = "ItemDamageOneHandCaster";
                        }
                        else
                        {
                            targetDamageDB = "ItemDamageOneHand";
                        }
                        break;
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
                            targetDamageDB = "ItemDamageTwoHandCaster";
                        }
                        else
                        {
                            targetDamageDB = "ItemDamageTwoHand";
                        }
                        break;
                    case 14: //	14: 'Miscellaneous',
                        targetDamageDB = "ItemDamageOneHandCaster";
                        break;
                }

                // Use . as decimal separator
                NumberFormatInfo nfi = new NumberFormatInfo();
                nfi.NumberDecimalSeparator = ".";

                result.itemLevel = (ushort)itemSparseEntry["ItemLevel"];
                result.overallQualityID = (byte)itemSparseEntry["OverallQualityID"];

                var damageRecord = await FindRecords(targetDamageDB, build, "ItemLevel", result.itemLevel);
                var itemDamage = damageRecord[0].FieldAs<float[]>("Quality")[result.overallQualityID];
                var dmgVariance = (float)itemSparseEntry["DmgVariance"];

                result.hasSparse = true;
                result.name = (string)itemSparseEntry["Display_lang"];
                result.expansionID = (byte)itemSparseEntry["ExpansionID"];
                result.speed = itemDelay.ToString("F2", nfi);
                result.dps = itemDamage.ToString("F2", nfi);
                result.minDamage = Math.Floor(itemDamage * itemDelay * (1 - dmgVariance * 0.5)).ToString();
                result.maxDamage = Math.Floor(itemDamage * itemDelay * (1 + dmgVariance * 0.5)).ToString();
            }

            var itemEffectEntries = await FindRecords("ItemEffect", build, "ParentItemID", itemID);
            if(itemEffectEntries.Count > 0)
            {
                result.itemEffects = new TTItemEffect[itemEffectEntries.Count];
                for (var i = 0; i < itemEffectEntries.Count; i++)
                {
                    result.itemEffects[i].spell = new TTSpell { spellID = (int)itemEffectEntries[i]["SpellID"] };
                    result.itemEffects[i].triggerType = (sbyte)itemEffectEntries[i]["TriggerType"];
                }
            }

            /* Fixups */
            // Classic ExpansionID column has 254, make 0. ¯\_(ツ)_/¯
            if (result.expansionID == 254)
                result.expansionID = 0;

            return Ok(result);
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