using System;
using System.Collections.Generic;
using System.Linq;

namespace WoWTools.SpellDescParser
{
    public class SpellDescParser
    {
        private readonly string input;
        private int cursor;
        public Root root;

        public SpellDescParser(string input)
        {
            this.input = input;
            this.cursor = 0;
            this.root = new Root(new List<INode>());
        }

        public void Parse()
        {
            var stringBuffer = "";

            while (cursor < input.Length)
            {
                var character = ReadChar();

                INode nodeToAdd = null;

                if (character == '$')
                {
                    nodeToAdd = ReadVariable();
                }
                else
                {
                    stringBuffer += character;
                }

                if (nodeToAdd != null)
                {
                    if (stringBuffer != string.Empty)
                    {
                        root.AddNode(new PlainText(stringBuffer));
                        stringBuffer = "";
                    }

                    root.AddNode(nodeToAdd);
                }
            }

            if (stringBuffer != string.Empty)
                root.AddNode(new PlainText(stringBuffer));
        }

        char ReadChar()
        {
            return input[cursor++];
        }

        public int? ReadInt()
        {
            var stringBuffer = "";

            while (input.Length > cursor && ((stringBuffer == "" && PeekChar() == '-') || char.IsDigit(PeekChar())))
            {
                stringBuffer += ReadChar();
            }

            if (int.TryParse(stringBuffer, out int result))
            {
                return result;
            }

            return null;
        }

        public uint? ReadUInt()
        {
            var stringBuffer = "";

            while (input.Length > cursor && ((stringBuffer == "" && PeekChar() == '-') || char.IsDigit(PeekChar())))
            {
                stringBuffer += ReadChar();
            }

            if (uint.TryParse(stringBuffer, out uint result))
            {
                return result;
            }
            
            return null;
        }

        public char PeekChar()
        {
            return input[cursor];
        }

        public INode ReadVariable()
        {
            // Special cases
            switch (PeekChar())
            {
                case '{':
                    return ReadExpression();
            }

            int? spellID = ReadInt();

            var type = PropertyType.Unknown;

            var startPos = cursor;

            var variableIdentifier = ReadChar();

            // Is this a way to detect longer variables? -- No

            // Broken long bois
            /*
             *  $@seplldesc
             *  $@speldesc
             *  $@@spelldesc
             */

            var multipleCharacterVariable = variableIdentifier.ToString();

            while (input.Length > cursor && (char.IsLetter(PeekChar()) || PeekChar() == '<' || PeekChar() == '>' || PeekChar() == '@'))
            {
                if (multipleCharacterVariable.ToLower() == "l" || multipleCharacterVariable.ToLower() == "g")
                    break;

                multipleCharacterVariable += ReadChar();
            }

            if (multipleCharacterVariable.Trim().Length > 1)
            {
                switch (multipleCharacterVariable)
                {
                    case "@spellname":
                    case "@spellnamme":
                    case "@spelnamme":
                    case "spellname":
                        type = PropertyType.SpellName;
                        spellID = ReadInt();
                        break;
                    case "@spelldesc":
                    case "@@spelldesc":
                    case "@spellDesc":
                    case "@Spelldesc":
                    case "@speldesc":
                    case "@seplldesc":
                    case "@spelldec":
                    case "@spellesc":
                    case "@pelldesc":
                    case "spelldesc@":
                    case "spellesc":
                    case "spelldesc":
                        type = PropertyType.SpellDescription;
                        spellID = ReadInt();
                        break;
                    case "@spellicon":
                        type = PropertyType.SpellIcon;
                        spellID = ReadInt();
                        break;
                    case "@spelltooltip":
                        type = PropertyType.SpellTooltip;
                        spellID = ReadInt();
                        break;
                    case "@lootspec":
                        type = PropertyType.LootSpec;
                        break;
                    case "@spellid":
                    case "@spellaura":
                    case "@garrabdesc":
                    case "@garrbuilding":
                    case "?a":
                    case "?A":
                    case "?c":
                    case "?C":
                    case "?l":
                    case "?s":
                    case "?S":
                    case ":q":
                    case "ecix":
                    case "ec":
                    case "sw":
                    case "pri":
                        type = PropertyType.Unknown;
                        break;
                    default:
                        Console.WriteLine("Unhandled multichar variable identifier: " + multipleCharacterVariable);
                        break;
                }

                if (type == PropertyType.Unknown)
                {
                    return new PlainText("$" + multipleCharacterVariable);
                }

                return new Property(type, null, spellID);
            }

            switch (variableIdentifier)
            {
                case 'a': // Radius
                    type = PropertyType.Radius0;
                    break;
                case 'A':
                    type = PropertyType.Radius1;
                    break;
                case 'D': // Duration
                case 'd': // Duration
                    type = PropertyType.Duration;
                    break;
                case 'E': // 'x per point' SpellEffect.EffectAmplitude
                case 'e': // 'x per point' SpellEffect.EffectAmplitude
                    type = PropertyType.EffectAmplitude;
                    break;
                case 'i': // Max Targets (SpellTargetRestrictions.MaxTargets)
                case 'I': // Max Targets (SpellTargetRestrictions.MaxTargets)
                    type = PropertyType.MaxTargets;
                    break;
                case 'h': // Proc chance (SpellAuraOptions.ProcChance)
                case 'H': // Proc chance (SpellAuraOptions.ProcChance)
                    type = PropertyType.ProcChance;
                    break;
                case 'n': // Proc charges (SpellAuraOptions.ProcCharges)
                case 'N': // Proc charges (SpellAuraOptions.ProcCharges)
                    type = PropertyType.ProcCharges;
                    break;
                case 'r': // SpellRange::ID
                    type = PropertyType.MinRange;
                    break;
                case 'R': // SpellRange::ID
                    type = PropertyType.MaxRange;
                    break;
                case 's': // Effect
                    type = PropertyType.Effect;
                    break;
                case 't': // Aura period
                case 'T': // Aura period
                    type = PropertyType.AuraPeriod;
                    break;
                case 'u': // Max stacks
                case 'U': // Max stacks
                    type = PropertyType.MaxStacks;
                    break;
                case 'v': // Max target level (SpellTargetRestrictions.MaxTargetLevel)
                case 'V': // Max target level (SpellTargetRestrictions.MaxTargetLevel)
                    type = PropertyType.MaxTargetLevel;
                    break;
                case 'x': // SpellEffect.EffectChainTargets
                case 'X': // SpellEffect.EffectChainTargets
                    type = PropertyType.ChainTargets;
                    break;
                case 'z': // Hearthstone location
                    type = PropertyType.HearthstoneLocation;
                    break;
                // TODO: Implement
                case 'b': // % chance per combo point for spell 14161. Broken in all other spells.
                case 'B': // See above.
                case 'c': // TODO: Investigate
                case 'm': // TODO: Investigate
                case 'M': // TODO: Investigate
                case 'o': // TODO: Investigate
                case 'O': // TODO: Investigate
                case 'p': // TODO: Investigate, appears to be 0 for some spells I checked rq
                case 'q': // TODO: Investigate, broken in only spell it is used: 39794
                case 'S': // EffectPoints...2? TODO: Investigate
                case 'w': // Another EffectBasePoints?? TODO: Investigate 118078
                case 'y': // Not parsed in-game?
                case 'f': // SpellEffect.EffectChainAmplitude (expression only)
                case 'F': // SpellEffect.EffectChainAmplitude (expression only)
                // These need special handling
                case 'g': // Gender conditional
                case 'G': // Gender conditional
                case 'C': // Specialization conditional 1 = 1st spec, etc
                case 'l': // Plurality
                case 'L': // Plurality
                case '?': // Conditional
                case '<': // Variable
                case '/': // Math
                case '*': // Math
                case '@': // External var
                    type = PropertyType.Unknown;
                    break;
                default:
                    Console.WriteLine("Unhandled variable identifier: " + variableIdentifier);
                    break;
            }

            uint? index = ReadUInt();

            // TODO: How are new lines handled???
            if (type == PropertyType.Unknown)
                return new PlainText("$" + spellID + variableIdentifier + index);

            return new Property(type, index, spellID);
        }
      
        public Expression ReadExpression()
        {
            // Skip over {
            cursor++;

            var startPos = cursor;
            var bracketEnd = input.IndexOf('}', cursor);
            if (bracketEnd == -1)
            {
                bracketEnd = input.Length;
            }

            cursor = bracketEnd + 1;

            return new Expression(input.Substring(startPos, bracketEnd - startPos));
        }
    }
}
