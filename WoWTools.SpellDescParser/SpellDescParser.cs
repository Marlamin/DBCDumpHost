using System;
using System.Collections.Generic;

namespace WoWTools.SpellDescParser
{
    public class SpellDescParser
    {
        private string input;
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

        public int ReadInt()
        {
            var stringBuffer = "";

            while (input.Length > cursor && ((stringBuffer == "" && PeekChar() == '-') || char.IsDigit(PeekChar())))
            {
                stringBuffer += ReadChar();
            }

            return int.Parse(stringBuffer);
        }

        public uint ReadUInt()
        {
            var stringBuffer = "";

            while (input.Length > cursor && ((stringBuffer == "" && PeekChar() == '-') || char.IsDigit(PeekChar())))
            {
                stringBuffer += ReadChar();
            }

            return uint.Parse(stringBuffer);
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

            int? spellID = null;

            if (input.Length > cursor && char.IsDigit(PeekChar()))
            {
                spellID = ReadInt();
            }

            var type = PropertyType.Unknown;

            var startPos = cursor;

            var variableIdentifier = ReadChar();

            // Is this a way to detect longer variables? -- No
            //while (input.Length > cursor && (char.IsLetter(PeekChar()) || PeekChar() == '<' || PeekChar() == '>' || PeekChar() == '@'))
            //{
            //    variableIdentifier += ReadChar();

            //    // Plurality identifiers are immediately followed by the singular form
            //    if (variableIdentifier.ToLower() == "l" || variableIdentifier.ToLower() == "g")
            //        break;
            //}

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
                case 'i': // Max Targets (SpellTargetRestrictions.MaxTargets)
                case 'I': // Max Targets (SpellTargetRestrictions.MaxTargets)
                    type = PropertyType.MaxTargets;
                    break;
                case 'n': // Proc charges (SpellAuraOptions.ProcCharges)
                case 'N': // Proc charges (SpellAuraOptions.ProcCharges)
                    type = PropertyType.ProcCharges;
                    break;
                case 'r': // SpellRange::ID
                case 'R': // SpellRange::ID
                    type = PropertyType.Range;
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
                case 'e': // 'x per point' SpellEffect.EffectAmplitude
                case 'h': // Proc chance (SpellAuraOptions.ProcChance)
                case 'H': // Proc chance (SpellAuraOptions.ProcChance)
                case 'm': // TODO: Investigate
                case 'M': // TODO: Investigate
                case 'o': // TODO: Investigate
                case 'O': // TODO: Investigate
                case 'p': // TODO: Investigate, appears to be 0 for some spells I checked rq
                case 'q': // TODO: Investigate, broken in only spell it is used: 39794
                case 'S': // EffectPoints...2? TODO: Investigate
                case 'w': // Another EffectPoints?? TODO: Investigate
                case 'y': // Not parsed in-game?
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
                    Console.WriteLine('Unhandled variable identifier: ' + variableIdentifier);
                    break;
            }

            uint? index = null;

            if (input.Length > cursor && char.IsDigit(PeekChar()))
            {
                index = ReadUInt();
            }

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
