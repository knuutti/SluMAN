using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SluMAN
{
    /// <summary>
    /// A memory address, or a pointer chain to one, as typed in the memory window:
    /// <list type="bullet">
    /// <item><c>7B4CE0</c>: a plain address.</item>
    /// <item><c>[5EC654]+168</c>: read the 4-byte big-endian pointer at 5EC654, then add 0x168.</item>
    /// <item><c>[[5EC654]+44]+130</c>: pointers can be nested to any depth.</item>
    /// </list>
    /// All numbers are hexadecimal, with or without a 0x prefix. Offsets can be added or subtracted.
    /// </summary>
    public sealed class AddressExpression
    {
        // Either a plain number or a pointer read of an inner expression, plus an offset.
        private readonly uint baseAddress;
        private readonly AddressExpression pointerTo;
        private readonly long offset;

        private AddressExpression(uint baseAddress, AddressExpression pointerTo, long offset)
        {
            this.baseAddress = baseAddress;
            this.pointerTo = pointerTo;
            this.offset = offset;
        }

        /// <summary>True for a plain address, which never moves.</summary>
        public bool IsStatic => pointerTo == null;

        /// <summary>
        /// Parses an expression. Returns false, with a reason in <paramref name="error"/>, when it
        /// can't be read.
        /// </summary>
        public static bool TryParse(string text, out AddressExpression expression, out string error)
        {
            expression = null;
            error = "";
            string source = (text ?? "").Replace(" ", "");
            if (source == "")
            {
                error = "Enter an address, for example 7B4CE0.";
                return false;
            }

            int position = 0;
            try
            {
                expression = ParseExpression(source, ref position);
                if (position != source.Length)
                {
                    throw new FormatException($"Unexpected \"{source[position]}\".");
                }
                return true;
            }
            catch (FormatException ex)
            {
                expression = null;
                error = $"Couldn't read the address: {ex.Message} Use 7B4CE0 or [5EC654]+168.";
                return false;
            }
        }

        private static AddressExpression ParseExpression(string source, ref int position)
        {
            uint baseAddress = 0;
            AddressExpression pointerTo = null;

            if (position < source.Length && source[position] == '[')
            {
                position++;
                pointerTo = ParseExpression(source, ref position);
                if (position >= source.Length || source[position] != ']')
                {
                    throw new FormatException("A \"[\" has no matching \"]\".");
                }
                position++;
            }
            else
            {
                baseAddress = (uint)ParseHex(source, ref position);
            }

            long offset = 0;
            while (position < source.Length && (source[position] == '+' || source[position] == '-'))
            {
                bool subtract = source[position] == '-';
                position++;
                long value = ParseHex(source, ref position);
                offset += subtract ? -value : value;
            }

            return new AddressExpression(baseAddress, pointerTo, offset);
        }

        private static long ParseHex(string source, ref int position)
        {
            if (position + 1 < source.Length && source[position] == '0' && (source[position + 1] == 'x' || source[position + 1] == 'X'))
            {
                position += 2;
            }

            int start = position;
            while (position < source.Length && Uri.IsHexDigit(source[position]))
            {
                position++;
            }

            if (position == start)
            {
                throw new FormatException(position < source.Length ? $"Expected a hex number at \"{source[position]}\"." : "Expected a hex number at the end.");
            }
            if (position - start > 8)
            {
                throw new FormatException("A number is longer than 8 hex digits.");
            }

            return long.Parse(source.Substring(start, position - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Follows the chain. <paramref name="readPointer"/> reads a 4-byte big-endian pointer at an
        /// address. Returns false when a pointer on the way is null.
        /// </summary>
        public bool TryResolve(Func<uint, uint> readPointer, out uint address)
        {
            address = 0;
            uint start = baseAddress;
            if (pointerTo != null)
            {
                uint pointerAddress;
                if (!pointerTo.TryResolve(readPointer, out pointerAddress))
                {
                    return false;
                }
                start = readPointer(pointerAddress);
                if (start == 0)
                {
                    return false;
                }
            }
            address = unchecked((uint)(start + offset));
            return true;
        }

        /// <summary>The expression in a tidy form, such as "[5EC654]+168".</summary>
        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            if (pointerTo != null)
            {
                text.Append('[').Append(pointerTo).Append(']');
            }
            else
            {
                text.Append(baseAddress.ToString("X"));
            }

            if (offset > 0)
            {
                text.Append('+').Append(offset.ToString("X"));
            }
            else if (offset < 0)
            {
                text.Append('-').Append((-offset).ToString("X"));
            }
            return text.ToString();
        }
    }
}
