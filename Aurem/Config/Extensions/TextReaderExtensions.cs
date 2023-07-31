using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Config.Extensions
{
    /// <summary>
    /// Provides useful extensions to the TextReader class.
    /// </summary>
    internal static class TextReaderExtensions
    {
        private static readonly char[] whitespace = {' ', '\t', '\r', '\n'};

        /// <summary>
        /// Reads a contiguous non-whitespace string from the stream. Returns null if the TextReader is all whitespace, or if the end of the stream is reached before any non-whitespace characters are read.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static string? ReadWord(this TextReader reader)
        {
            StringBuilder sb = new StringBuilder();
            var ch = ' ';
            // eat whitespace
            while (whitespace.Any(x => x == ch) && ch != -1) ch = (char)reader.Read();

            if (ch == -1) return null;

            while (!whitespace.Any(x => x == ch) && ch != -1)
            {
                sb.Append(ch);
                ch = (char)reader.Read();
            }

            if (sb.ToString() == "") return null;

            return sb.ToString();
        }
    }
}
