/* Generated By:JavaCC: Do not edit this line. TokenMgrError.java Version 5.0 */
/* JavaCCOptions: */
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
    /// <summary>Token Manager Error.</summary>
    public class TokenMgrError : Exception
    {
        /// <summary>
        /// The version identifier for this Serializable class.
        /// Increment only if the <i>serialized</i> form of the
        /// class changes.
        /// </summary>
        private const long serialVersionUID = 1L;

        // Ordinals for various reasons why an Error of this type can be thrown.
        
        /// <summary>
        /// Lexical error occurred.
        /// </summary>
        internal const int LEXICAL_ERROR = 0;

        /// <summary>
        /// An attempt was made to create a second instance of a static token manager.
        /// </summary>
        internal const int STATIC_LEXER_ERROR = 1;

        /// <summary>
        /// Tried to change to an invalid lexical state.
        /// </summary>
        internal const int INVALID_LEXICAL_STATE = 2;

        /// <summary>
        /// Detected (and bailed out of) an infinite loop in the token manager.
        /// </summary>
        internal const int LOOP_DETECTED = 3;

        /// <summary>
        /// Indicates the reason why the exception is thrown. It will have
        /// one of the above 4 values.
        /// </summary>
        internal int errorCode;

        /// <summary>
        /// Replaces unprintable characters by their escaped (or unicode escaped)
        /// equivalents in the given string
        /// </summary>
        protected static string AddEscapes(string str)
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

        /// <summary>
        /// Returns a detailed message for the Error when it is thrown by the
        /// token manager to indicate a lexical error.
        /// Parameters :
        ///    EOFSeen     : indicates if EOF caused the lexical error
        ///    curLexState : lexical state in which this error occurred
        ///    errorLine   : line number when the error occurred
        ///    errorColumn : column number when the error occurred
        ///    errorAfter  : prefix that was seen before this error occurred
        ///    curchar     : the offending character
        /// Note: You can customize the lexical error message by modifying this method.
        /// </summary>
        protected static string LexicalError(bool EOFSeen, int lexState, int errorLine, int errorColumn, string errorAfter, char curChar)
        {
            return ("Lexical error at line " +
                  errorLine + ", column " +
                  errorColumn + ".  Encountered: " +
                  (EOFSeen ? "<EOF> " : ("\"" + AddEscapes(curChar.ToString()) + "\"") + " (" + (int)curChar + "), ") +
                  "after : \"" + AddEscapes(errorAfter) + "\"");
        }

        /// <summary>
        /// You can also modify the body of this method to customize your error messages.
        /// For example, cases like LOOP_DETECTED and INVALID_LEXICAL_STATE are not
        /// of end-users concern, so you can return something like :
        ///
        ///     "Internal Error : Please file a bug report .... "
        ///
        /// from this method for such cases in the release version of your parser.
        /// </summary>
        public override string Message => base.Message;

        // Constructors of various flavors follow.
        
        /// <summary>No arg constructor.</summary>
        public TokenMgrError()
        {
        }

        /// <summary>Constructor with message and reason.</summary>
        public TokenMgrError(string message, int reason)
            : base(message)
        {
            errorCode = reason;
        }

        /// <summary>Full Constructor.</summary>
        public TokenMgrError(bool EOFSeen, int lexState, int errorLine, int errorColumn, string errorAfter, char curChar, int reason)
            : this(LexicalError(EOFSeen, lexState, errorLine, errorColumn, errorAfter, curChar), reason)
        { }
    }
}
