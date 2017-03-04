/* Generated By:JavaCC: Do not edit this line. ParseException.java Version 5.0 */
/* JavaCCOptions:KEEP_LINE_COL=null */
/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 * (or see http://www.gnu.org/copyleft/lesser.html)
 */
using System;
using System.Text;

namespace NCDK.Smiles.SMARTS.Parser
{

    /// <summary>
    /// This exception is thrown when parse errors are encountered.
    /// You can explicitly create objects of this exception type by
    /// calling the method generateParseException in the generated
    /// parser.
    ///
    /// You can modify this class to customize your error reporting
    /// mechanisms so long as you retain the public fields.
    /// </summary>
    public class ParseException : System.Exception
    {
        /// <summary>
        /// This constructor is used by the method "generateParseException"
        /// in the generated parser.  Calling this constructor generates
        /// a new object of this type with the fields "currentToken",
        /// "expectedTokenSequences", and "tokenImage" set.
        /// </summary>
        public ParseException(Token currentTokenVal,
                              int[][] expectedTokenSequencesVal,
                              string[] tokenImageVal
                             )
          : base(Initialise(currentTokenVal, expectedTokenSequencesVal, tokenImageVal))
        {
            currentToken = currentTokenVal;
            expectedTokenSequences = expectedTokenSequencesVal;
            tokenImage = tokenImageVal;
        }

        /// <summary>
        /// The following constructors are for use by you for whatever
        /// purpose you can think of.  Constructing the exception in this
        /// manner makes the exception behave in the normal way - i.e., as
        /// documented in the class "Throwable".  The fields "errorToken",
        /// "expectedTokenSequences", and "tokenImage" do not contain
        /// relevant information.  The JavaCC generated code does not use
        /// these constructors.
        /// </summary>

        public ParseException()
                  : base()
        { }

        /// <summary>Constructor with message.</summary>
        public ParseException(string message)
              : base(message)
        { }

        /// <summary>
        /// This is the last token that has been consumed successfully.  If
        /// this object has been created due to a parse error, the token
        /// followng this token will (therefore) be the first error token.
        /// </summary>
        public Token currentToken;

        /// <summary>
        /// Each entry in this array is an array of integers.  Each array
        /// of integers represents a sequence of tokens (by their ordinal
        /// values) that is expected at this point of the parse.
        /// </summary>
        public int[][] expectedTokenSequences;

        /// <summary>
        /// This is a reference to the "tokenImage" array of the generated
        /// parser within which the parse error occurred.  This array is
        /// defined in the generated ...Constants interface.
        /// </summary>
        public string[] tokenImage;

        /// <summary>
        /// It uses "currentToken" and "expectedTokenSequences" to generate a parse
        /// error message and returns it.  If this object has been created
        /// due to a parse error, and you do not catch it (it gets thrown
        /// from the parser) the correct error message
        /// gets displayed.
        /// </summary>
        private static string Initialise(Token currentToken,
                                 int[][] expectedTokenSequences,
                                 string[] tokenImage)
        {
            string eol = System.Environment.NewLine;
            StringBuilder expected = new StringBuilder();
            int maxSize = 0;
            for (int i = 0; i < expectedTokenSequences.Length; i++)
            {
                if (maxSize < expectedTokenSequences[i].Length)
                {
                    maxSize = expectedTokenSequences[i].Length;
                }
                for (int j = 0; j < expectedTokenSequences[i].Length; j++)
                {
                    expected.Append(tokenImage[expectedTokenSequences[i][j]]).Append(' ');
                }
                if (expectedTokenSequences[i][expectedTokenSequences[i].Length - 1] != 0)
                {
                    expected.Append("...");
                }
                expected.Append(eol).Append("    ");
            }
            string retval = "Encountered \"";
            Token tok = currentToken.next;
            for (int i = 0; i < maxSize; i++)
            {
                if (i != 0) retval += " ";
                if (tok.kind == 0)
                {
                    retval += tokenImage[0];
                    break;
                }
                retval += " " + tokenImage[tok.kind];
                retval += " \"";
                retval += Add_escapes(tok.image);
                retval += " \"";
                tok = tok.next;
            }
            retval += "\" at line " + currentToken.next.beginLine + ", column " + currentToken.next.beginColumn;
            retval += "." + eol;
            if (expectedTokenSequences.Length == 1)
            {
                retval += "Was expecting:" + eol + "    ";
            }
            else
            {
                retval += "Was expecting one of:" + eol + "    ";
            }
            retval += expected.ToString();
            return retval;
        }

        /// <summary>
        /// The end of line string for this machine.
        /// </summary>
        protected string eol = System.Environment.NewLine;

        /// <summary>
        /// Used to convert raw characters to their escaped version
        /// when these raw version cannot be used as part of an ASCII
        /// string literal.
        /// </summary>
        static string Add_escapes(string str)
        {
            StringBuilder retval = new StringBuilder();
            char ch;
            for (int i = 0; i < str.Length; i++)
            {
                switch (str[i])
                {
                    case '\0':
                        continue;
                    case '\b':
                        retval.Append("\\b");
                        continue;
                    case '\t':
                        retval.Append("\\t");
                        continue;
                    case '\n':
                        retval.Append("\\n");
                        continue;
                    case '\f':
                        retval.Append("\\f");
                        continue;
                    case '\r':
                        retval.Append("\\r");
                        continue;
                    case '\"':
                        retval.Append("\\\"");
                        continue;
                    case '\'':
                        retval.Append("\\\'");
                        continue;
                    case '\\':
                        retval.Append("\\\\");
                        continue;
                    default:
                        if ((ch = str[i]) < 0x20 || ch > 0x7e)
                        {
                            string s = "0000" + Convert.ToString(ch, 16);
                            retval.Append("\\u" + s.Substring(s.Length - 4));
                        }
                        else
                        {
                            retval.Append(ch);
                        }
                        continue;
                }
            }
            return retval.ToString();
        }
    }
}
