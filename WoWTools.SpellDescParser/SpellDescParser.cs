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

            // Is this a way to detect longer variables? 
            if (char.IsLower(PeekChar()))
            {
                // EC
                // ECIX
                // PRI
                // 
            }

            switch (variableIdentifier)
            {

                case 'a': // Radius
                    type = PropertyType.Radius0;
                    break;
                case 'A':
                    type = PropertyType.Radius1;
                    break;
                case 'd': // Duration
                    type = PropertyType.Duration;
                    break;
                case 's': // Effect
                    type = PropertyType.Effect;
                    break;
                case 'u': // Max stacks
                case 'U': // Max stacks
                    type = PropertyType.MaxStacks;
                    break;
                case 'z': // Hearthstone location
                    type = PropertyType.HearthstoneLocation;
                    break;
                // TODO: Implement
                case 'b': // % chance per combo point for spell 14161. Broken in all other spells.
                case 'B': // See above.
                case 'c': // TODO: Investigate
                case 'C': // Specialization conditional 1 = 1st spec, etc
                case 'D': // Duration
                case 'e': // "x per point"
                case 'h': // Proc chance (SpellAuraOptions.ProcChance)
                case 'H': // Proc chance (SpellAuraOptions.ProcChance)
                case 'i': // Max Targets (SpellTargetRestrictions.MaxTargets)
                case 'I': // Max Targets (SpellTargetRestrictions.MaxTargets)
                case 'm': // TODO: Investigate
                case 'M': // TODO: Investigate
                case 'n': // Proc charges (SpellAuraOptions.ProcCharges)
                case 'N': // Proc charges (SpellAuraOptions.ProcCharges)
                case 'o': // TODO: Investigate
                case 'O': // TODO: Investigate
                case 'p': // TODO: Investigate, appears to be 0 for some spells I checked rq
                case 'q': // TODO: Investigate, broken in only spell it is used: 39794
                case 'r': // Range?? TODO: Check if capital changes array index in SpellRange
                case 'R': // Range?? TODO: Check if capital changes array index in SpellRange
                case 'S': // EffectPoints...2? TODO: Investigate
                case 't': // SpellEffect.AuraPeriod
                case 'T': // SpellEffect.AuraPeriod
                case 'v': // Max target level (SpellTargetRestrictions.MaxTargetLevel
                case 'V': // Max target level (SpellTargetRestrictions.MaxTargetLevel
                case 'w': // Another EffectPoints?? TODO: Investigate
                case 'x': // SpellEffect.EffectChainTargets
                case 'X': // SpellEffect.EffectChainTargets
                case 'y': // Not parsed in-game?
                // These need special handling
                case 'g': // Gender conditional
                case 'G': // Gender conditional
                case 'l': // Plurality
                case 'L': // Plurality
                case '?': // Conditional
                case '<': // Variable
                case '/': // Math
                case '*': // Math
                case '@': // External var
                    type = PropertyType.Unknown;
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
