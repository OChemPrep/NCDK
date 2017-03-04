/* Generated By:JavaCC: Do not edit this line. SimpleCharStream.java Version 5.0 */
/* JavaCCOptions:STATIC=false,SUPPORT_CLASS_VISIBILITY_PUBLIC=true */
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
namespace NCDK.Smiles.SMARTS.Parser
{

    /// <summary>
    /// An implementation of interface CharStream, where the stream is assumed to
    /// contain only ASCII characters (without unicode processing).
    /// </summary>

    public class SimpleCharStream
    {
        /// <summary>Whether parser is static.</summary>
        public const bool staticFlag = false;
        int bufsize;
        int available;
        int tokenBegin;
        /// <summary>Position in buffer.</summary>
        public int bufpos = -1;
        protected int[] bufline;
        protected int[] bufcolumn;

        protected int column = 0;
        protected int line = 1;

        protected bool prevCharIsCR = false;
        protected bool prevCharIsLF = false;

        protected System.IO.TextReader inputStream;

        protected char[] buffer;
        protected int maxNextCharInd = 0;
        protected int inBuf = 0;
        protected int tabSize = 8;

        protected void SetTabSize(int i) { tabSize = i; }
        protected int GetTabSize(int i) { return tabSize; }


        protected void ExpandBuff(bool wrapAround)
        {
            char[] newbuffer = new char[bufsize + 2048];
            int[] newbufline = new int[bufsize + 2048];
            int[] newbufcolumn = new int[bufsize + 2048];

            try
            {
                if (wrapAround)
                {
                    System.Array.Copy(buffer, tokenBegin, newbuffer, 0, bufsize - tokenBegin);
                    System.Array.Copy(buffer, 0, newbuffer, bufsize - tokenBegin, bufpos);
                    buffer = newbuffer;

                    System.Array.Copy(bufline, tokenBegin, newbufline, 0, bufsize - tokenBegin);
                    System.Array.Copy(bufline, 0, newbufline, bufsize - tokenBegin, bufpos);
                    bufline = newbufline;

                    System.Array.Copy(bufcolumn, tokenBegin, newbufcolumn, 0, bufsize - tokenBegin);
                    System.Array.Copy(bufcolumn, 0, newbufcolumn, bufsize - tokenBegin, bufpos);
                    bufcolumn = newbufcolumn;

                    maxNextCharInd = (bufpos += (bufsize - tokenBegin));
                }
                else
                {
                    System.Array.Copy(buffer, tokenBegin, newbuffer, 0, bufsize - tokenBegin);
                    buffer = newbuffer;

                    System.Array.Copy(bufline, tokenBegin, newbufline, 0, bufsize - tokenBegin);
                    bufline = newbufline;

                    System.Array.Copy(bufcolumn, tokenBegin, newbufcolumn, 0, bufsize - tokenBegin);
                    bufcolumn = newbufcolumn;

                    maxNextCharInd = (bufpos -= tokenBegin);
                }
            }
            catch (System.Exception t)
            {
                throw new System.Exception(t.Message);
            }


            bufsize += 2048;
            available = bufsize;
            tokenBegin = 0;
        }

        protected void FillBuff()
        {
            if (maxNextCharInd == available)
            {
                if (available == bufsize)
                {
                    if (tokenBegin > 2048)
                    {
                        bufpos = maxNextCharInd = 0;
                        available = tokenBegin;
                    }
                    else if (tokenBegin < 0)
                        bufpos = maxNextCharInd = 0;
                    else
                        ExpandBuff(false);
                }
                else if (available > tokenBegin)
                    available = bufsize;
                else if ((tokenBegin - available) < 2048)
                    ExpandBuff(true);
                else
                    available = tokenBegin;
            }

            int i;
            try
            {
                if ((i = inputStream.Read(buffer, maxNextCharInd, available - maxNextCharInd)) == 0)
                {
                    inputStream.Close();
                    throw new System.IO.IOException();
                }
                else
                    maxNextCharInd += i;
                return;
            }
            catch (System.Exception e)
            {
                if (e is System.ObjectDisposedException)
                    e = new System.IO.IOException(e.Message);
                if (e is System.IO.IOException)
                {
                    --bufpos;
                    Backup(0);
                    if (tokenBegin == -1)
                        tokenBegin = bufpos;
                }
                throw e;
            }
        }

        /// <summary>Start.</summary>
        public char BeginToken()
        {
            tokenBegin = -1;
            char c = ReadChar();
            tokenBegin = bufpos;

            return c;
        }

        protected void UpdateLineColumn(char c)
        {
            column++;

            if (prevCharIsLF)
            {
                prevCharIsLF = false;
                line += (column = 1);
            }
            else if (prevCharIsCR)
            {
                prevCharIsCR = false;
                if (c == '\n')
                {
                    prevCharIsLF = true;
                }
                else
                    line += (column = 1);
            }

            switch (c)
            {
                case '\r':
                    prevCharIsCR = true;
                    break;
                case '\n':
                    prevCharIsLF = true;
                    break;
                case '\t':
                    column--;
                    column += (tabSize - (column % tabSize));
                    break;
                default:
                    break;
            }

            bufline[bufpos] = line;
            bufcolumn[bufpos] = column;
        }

        /// <summary>Read a character.</summary>
        public char ReadChar()
        {
            if (inBuf > 0)
            {
                --inBuf;

                if (++bufpos == bufsize)
                    bufpos = 0;

                return buffer[bufpos];
            }

            if (++bufpos >= maxNextCharInd)
                FillBuff();

            char c = buffer[bufpos];

            UpdateLineColumn(c);
            return c;
        }

        [System.Obsolete()]
        /// <summary>
        // @deprecated
        /// <seealso cref="getEndColumn"/>
        /// </summary>

        public int GetColumn()
        {
            return bufcolumn[bufpos];
        }

        [System.Obsolete()]
        /// <summary>
        // @deprecated
        /// <seealso cref="getEndLine"/>
        /// </summary>

        public int GetLine()
        {
            return bufline[bufpos];
        }

        /// <summary>Get token end column number.</summary>
        public int GetEndColumn()
        {
            return bufcolumn[bufpos];
        }

        /// <summary>Get token end line number.</summary>
        public int GetEndLine()
        {
            return bufline[bufpos];
        }

        /// <summary>Get token beginning column number.</summary>
        public int GetBeginColumn()
        {
            return bufcolumn[tokenBegin];
        }

        /// <summary>Get token beginning line number.</summary>
        public int GetBeginLine()
        {
            return bufline[tokenBegin];
        }

        /// <summary>Backup a number of characters.</summary>
        public void Backup(int amount)
        {

            inBuf += amount;
            if ((bufpos -= amount) < 0)
                bufpos += bufsize;
        }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.TextReader dstream, int startline,
        int startcolumn, int buffersize)
        {
            inputStream = dstream;
            line = startline;
            column = startcolumn - 1;

            available = bufsize = buffersize;
            buffer = new char[buffersize];
            bufline = new int[buffersize];
            bufcolumn = new int[buffersize];
        }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.TextReader dstream, int startline,
                                int startcolumn)
           : this(dstream, startline, startcolumn, 4096)
        { }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.TextReader dstream)
        : this(dstream, 1, 1, 4096)
        { }

        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.TextReader dstream, int startline,
        int startcolumn, int buffersize)
        {
            inputStream = dstream;
            line = startline;
            column = startcolumn - 1;

            if (buffer == null || buffersize != buffer.Length)
            {
                available = bufsize = buffersize;
                buffer = new char[buffersize];
                bufline = new int[buffersize];
                bufcolumn = new int[buffersize];
            }
            prevCharIsLF = prevCharIsCR = false;
            tokenBegin = inBuf = maxNextCharInd = 0;
            bufpos = -1;
        }

        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.TextReader dstream, int startline,
                           int startcolumn)
        {
            ReInit(dstream, startline, startcolumn, 4096);
        }

        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.TextReader dstream)
        {
            ReInit(dstream, 1, 1, 4096);
        }
        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.Stream dstream, string encoding, int startline,
        int startcolumn, int buffersize)
          : this(encoding == null ? new System.IO.StreamReader(dstream) : new System.IO.StreamReader(dstream, System.Text.Encoding.GetEncoding(encoding)), startline, startcolumn, buffersize)
        { }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.Stream dstream, int startline,
        int startcolumn, int buffersize)
          : this(new System.IO.StreamReader(dstream), startline, startcolumn, buffersize)
        { }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.Stream dstream, string encoding, int startline,
                                int startcolumn)
          : this(dstream, encoding, startline, startcolumn, 4096)
        { }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.Stream dstream, int startline,
                                int startcolumn)
        : this(dstream, startline, startcolumn, 4096)
        { }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.Stream dstream, string encoding)
       : this(dstream, encoding, 1, 1, 4096)
        { }

        /// <summary>Constructor.</summary>
        public SimpleCharStream(System.IO.Stream dstream)
          : this(dstream, 1, 1, 4096)
        { }

        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.Stream dstream, string encoding, int startline,
                                int startcolumn, int buffersize)
        {
            ReInit(encoding == null ? new System.IO.StreamReader(dstream) : new System.IO.StreamReader(dstream, System.Text.Encoding.GetEncoding(encoding)), startline, startcolumn, buffersize);
        }

        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.Stream dstream, int startline,
                                int startcolumn, int buffersize)
        {
            ReInit(new System.IO.StreamReader(dstream), startline, startcolumn, buffersize);
        }

        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.Stream dstream, string encoding)
        {
            ReInit(dstream, encoding, 1, 1, 4096);
        }

        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.Stream dstream)
        {
            ReInit(dstream, 1, 1, 4096);
        }
        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.Stream dstream, string encoding, int startline,
                           int startcolumn)
        {
            ReInit(dstream, encoding, startline, startcolumn, 4096);
        }
        /// <summary>Reinitialise.</summary>
        public void ReInit(System.IO.Stream dstream, int startline,
                           int startcolumn)
        {
            ReInit(dstream, startline, startcolumn, 4096);
        }
        /// <summary>Get token literal value.</summary>
        public string GetImage()
        {
            if (bufpos >= tokenBegin)
                return new string(buffer, tokenBegin, bufpos - tokenBegin + 1);
            else
                return new string(buffer, tokenBegin, bufsize - tokenBegin) +
                                      new string(buffer, 0, bufpos + 1);
        }

        /// <summary>Get the suffix.</summary>
        public char[] GetSuffix(int len)
        {
            char[] ret = new char[len];

            if ((bufpos + 1) >= len)
                System.Array.Copy(buffer, bufpos - len + 1, ret, 0, len);
            else
            {
                System.Array.Copy(buffer, bufsize - (len - bufpos - 1), ret, 0,
                                                                  len - bufpos - 1);
                System.Array.Copy(buffer, 0, ret, len - bufpos - 1, bufpos + 1);
            }

            return ret;
        }

        /// <summary>Reset buffer when finished.</summary>
        public void Done()
        {
            buffer = null;
            bufline = null;
            bufcolumn = null;
        }

        /// <summary>
        /// Method to adjust line and column numbers for the start of a token.
        /// </summary>
        public void AdjustBeginLineColumn(int newLine, int newCol)
        {
            int start = tokenBegin;
            int len;

            if (bufpos >= tokenBegin)
            {
                len = bufpos - tokenBegin + inBuf + 1;
            }
            else
            {
                len = bufsize - tokenBegin + bufpos + 1 + inBuf;
            }

            int i = 0, j = 0, k = 0;
            int nextColDiff = 0, columnDiff = 0;

            while (i < len && bufline[j = start % bufsize] == bufline[k = ++start % bufsize])
            {
                bufline[j] = newLine;
                nextColDiff = columnDiff + bufcolumn[k] - bufcolumn[j];
                bufcolumn[j] = newCol + columnDiff;
                columnDiff = nextColDiff;
                i++;
            }

            if (i < len)
            {
                bufline[j] = newLine++;
                bufcolumn[j] = newCol + columnDiff;

                while (i++ < len)
                {
                    if (bufline[j = start % bufsize] != bufline[++start % bufsize])
                        bufline[j] = newLine++;
                    else
                        bufline[j] = newLine;
                }
            }

            line = bufline[j];
            column = bufcolumn[j];
        }
    }
}
