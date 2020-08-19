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
            if (PeekChar() == '{')
            {
                return ReadExpression();
            }

            int? spellID = null;

            if (input.Length > cursor && char.IsDigit(PeekChar()))
            {
                spellID = ReadInt();
            }

            var type = PropertyType.Unknown;

            var variableIdentifier = ReadChar();

            switch (variableIdentifier)
            {
                case 'd': // Duration
                    type = PropertyType.Duration;
                    break;
                case 's': // Effect
                    type = PropertyType.Effect;
                    break;
                case 'a': // Radius
                    type = PropertyType.Radius0;
                    break;
                case 'A':
                    type = PropertyType.Radius1;
                    break;
                case '?': // Conditional
                case '<':
                case 'l':
                case 't':
                case 'o':
                case 'm':
                case 'M':
                case '/': // Math
                case '*': // Math
                case '@': // External var?
                case 'z':
                case 'u':
                case 'n':
                case 'I':
                case 'e':
                case 'h':
                case 'r':
                case 'g':
                case 'x':
                case 'i':
                case 'L':
                case 'b':
                case 'T':
                case 'w':
                case 'p':
                case 'D':
                case 'G':
                case 'v':
                case 'q':
                case 'R':
                case 'U':
                case 'H':
                case 'S':
                case 'c':
                case 'C':
                case 'N':
                case 'P':
                case 'O':
                case 'V':
                case 'E':
                case 'y':
                case 'B':
                case 'X':
                    type = PropertyType.Unknown;
                    break;
            }

            uint? index = null;

            if (input.Length > cursor && char.IsDigit(PeekChar()))
            {
                index = ReadUInt();
            }

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
