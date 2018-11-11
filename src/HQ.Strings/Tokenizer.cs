#region LICENSE

// Unless explicitly acquired and licensed from Licensor under another
// license, the contents of this file are subject to the Reciprocal Public
// License ("RPL") Version 1.5, or subsequent versions as allowed by the RPL,
// and You may not copy or use this file in either source code or executable
// form, except in compliance with the terms and conditions of the RPL.
// 
// All software distributed under the RPL is provided strictly on an "AS
// IS" basis, WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, AND
// LICENSOR HEREBY DISCLAIMS ALL SUCH WARRANTIES, INCLUDING WITHOUT
// LIMITATION, ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE, QUIET ENJOYMENT, OR NON-INFRINGEMENT. See the RPL for specific
// language governing rights and limitations under the RPL.

#endregion

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace HQ.Strings
{
    public static class Tokenizer
    {
        #region Reading

        public static List<string[]> Tokenize(string[] lines, List<string> errors, ref bool foundError)
        {
            var workingLine = new List<string>();
            var workingToken = new StringBuilder();


            var result = new List<string[]>(lines.Length);

            for (var lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                var input = lines[lineNumber];

                workingLine.Clear();
                workingToken.Clear();

                var inQuotes = false;
                var escape = false;

                for (var i = 0; i < input.Length; i++)
                {
                    var finishToken = false;

                    if (inQuotes)
                    {
                        if (escape)
                        {
                            escape = false;
                            switch (input[i])
                            {
                                case '\\':
                                    workingToken.Append('\\');
                                    break;
                                case 'n':
                                    workingToken.Append('\n');
                                    break;
                                case '\"':
                                    workingToken.Append('\"');
                                    break;

                                default:
                                    errors.Add("ERROR: Unknown escape token '" + input[i] + "', line " + lineNumber +
                                               ", position " + i + ".");
                                    foundError = true;
                                    break;
                            }
                        }
                        else
                        {
                            switch (input[i])
                            {
                                case '\\':
                                    escape = true;
                                    break;

                                case '\"':
                                    inQuotes = false;
                                    finishToken = true;
                                    break;

                                default:
                                    workingToken.Append(input[i]);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (char.IsWhiteSpace(input[i]))
                            finishToken = true;
                        else if (input[i] == '#')
                            break;
                        else if (input[i] == '\"')
                            inQuotes = true;
                        else
                            workingToken.Append(input[i]);
                    }

                    if (finishToken && workingToken.Length > 0)
                    {
                        workingLine.Add(workingToken.ToString());
                        workingToken.Clear();
                    }
                }

                // Handle final token:
                if (workingToken.Length > 0)
                {
                    workingLine.Add(workingToken.ToString());
                    workingToken.Clear();
                }

                if (workingLine.Count > 0)
                    result.Add(workingLine.ToArray());
            }

            return result;
        }

        #endregion

        #region Writing

        public static string AsToken(string token)
        {
            for (var i = 0; i < token.Length; i++)
                switch (token[i])
                {
                    case '#':
                    case '\\':
                    case '"':
                        return QuoteWrapAndEscape(token, i);

                    default:
                        if (char.IsWhiteSpace(token[i]))
                            return QuoteWrapAndEscape(token, i);
                        break;
                }

            return token;
        }

        private static string QuoteWrapAndEscape(string token, int startIndex)
        {
            var sb = new StringBuilder();

            sb.Append('"');
            sb.Append(token, 0, startIndex);
            for (var i = startIndex; i < token.Length; i++)
                switch (token[i])
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r': break;
                    default:
                        sb.Append(token[i]);
                        break;
                }
            sb.Append('"');

            return sb.ToString();
        }


        public static void WriteLine(StreamWriter stream, string firstToken, params string[] tokens)
        {
            stream.Write(firstToken);

            for (var t = 0; t < tokens.Length; t++)
            {
                stream.Write(' ');
                stream.Write(AsToken(tokens[t]));
            }

            stream.WriteLine();
        }

        public static void Write(StreamWriter stream, string firstToken, params string[] tokens)
        {
            stream.Write(firstToken);

            for (var t = 0; t < tokens.Length; t++)
            {
                stream.Write(' ');
                stream.Write(AsToken(tokens[t]));
            }

            stream.Write(' '); // <- in case more tokens are written
        }

        #endregion

        #region Binary Read/Write

        public static List<string[]> BinaryRead(BinaryReader br)
        {
            var lineCount = br.ReadInt32();
            var lines = new List<string[]>(lineCount);

            for (var l = 0; l < lineCount; l++)
            {
                var tokenCount = br.ReadInt32();
                var tokens = new string[tokenCount];
                for (var t = 0; t < tokenCount; t++) tokens[t] = br.ReadString();
                lines.Add(tokens);
            }

            return lines;
        }

        public static void BinaryWrite(BinaryWriter bw, List<string[]> tokenizedLines)
        {
            bw.Write(tokenizedLines.Count);
            foreach (var tokenizedLine in tokenizedLines)
            {
                bw.Write(tokenizedLine.Length);
                foreach (var token in tokenizedLine)
                {
                    Debug.Assert(token != null);
                    bw.Write(token);
                }
            }
        }

        #endregion
    }
}
