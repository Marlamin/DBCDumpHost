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
        public async Task<Dictionary<string, object>> GetItemTooltip(int itemID, string build)
        {
            var variables = new Dictionary<string, object>();

            // Make sure DBs are loaded
            var itemDB = await dbcManager.GetOrLoad("Item", build);
            var itemSparseDB = await dbcManager.GetOrLoad("ItemSparse", build);

            if(!itemDB.TryGetValue(itemID, out DBCDRow itemEntry))
            {
                throw new KeyNotFoundException("Unable to find ID " + itemID + " in Item.db2");
            }

            // TODO: Look in ItemModifiedAppearance => ItemAppearance for proper icon if icon in Item is 0.
            variables.Add("iconFileDataID", ((int)itemEntry["IconFileDataID"]).ToString());

            if (!itemSparseDB.TryGetValue(itemID, out DBCDRow itemSparseEntry))
            {
                var itemSearchNameDB = await dbcManager.GetOrLoad("ItemSearchName", build);
                if (!itemSearchNameDB.TryGetValue(itemID, out DBCDRow itemSearchNameEntry))
                {
                    throw new KeyNotFoundException("Unable to find ID " + itemID + " in ItemSearchName.db2 or ItemSparse.db2");
                }

                variables.Add("name", (string)itemSearchNameEntry["Display_lang"]);
                variables.Add("expansionID", (byte)itemSearchNameEntry["ExpansionID"]);
                variables.Add("itemLevel", (ushort)itemSearchNameEntry["ItemLevel"]);
                variables.Add("overallQualityID", (byte)itemSearchNameEntry["OverallQualityID"]);
                variables.Add("hasSparse", "false");
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

                var itemLevel = (ushort)itemSparseEntry["ItemLevel"];
                var overallQualityID = (byte)itemSparseEntry["OverallQualityID"];

                var damageRecord = await FindRecord(targetDamageDB, build, "ItemLevel", itemLevel);
                var itemDamage = damageRecord.FieldAs<float[]>("Quality")[overallQualityID];
                var dmgVariance = (float)itemSparseEntry["DmgVariance"];

                variables.Add("hasSparse", "true");
                variables.Add("name", (string)itemSparseEntry["Display_lang"]);
                variables.Add("expansionID", (byte)itemSparseEntry["ExpansionID"]);
                variables.Add("overallQualityID", overallQualityID);
                variables.Add("itemLevel", itemLevel);
                variables.Add("speed", itemDelay.ToString("F2", nfi));
                variables.Add("dps", itemDamage.ToString("F2", nfi));
                variables.Add("minDamage", Math.Floor(itemDamage * itemDelay * (1 - dmgVariance * 0.5)).ToString());
                variables.Add("maxDamage", Math.Floor(itemDamage * itemDelay * (1 + dmgVariance * 0.5)).ToString());
            }

            /* Fixups */
            // Classic ExpansionID column has 254, make 0. ¯\_(ツ)_/¯
            if ((byte)variables["expansionID"] == 254)
                variables["expansionID"] = 0;

            return variables;
        }

        [HttpGet("spell/{SpellID}")]
        public string GetSpellTooltip(int spellID)
        {
            return "Spell tooltip for " + spellID;
        }

        private async Task<DBCDRow> FindRecord(string name, string build, string col, int val)
        {
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
                            return row;
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
                            return row;
                    }
                }
            }

            throw new KeyNotFoundException("No record found matching specified key/value");
        }
    }
}