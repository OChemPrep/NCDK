/* Copyright (C) 2012  Gilleain Torrance <gilleain.torrance@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.Common.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace NCDK.Groups
{
    /// <summary>
    /// A permutation with some associated methods to multiply, invert, and convert
    /// to cycle strings. Much of the code in this was implemented from the
    /// C.A.G.E.S. book <token>cdk-cite-Kreher98</token>.
    /// </summary>
    // @author maclean
    // @cdk.module group
    public sealed class Permutation : IReadOnlyList<int>
    {
        /// <summary>
        /// Constructs an identity permutation with <paramref name="size"/> elements.
        /// </summary>
        /// <param name="size">the number of elements in the permutation</param>
        public Permutation(int size)
        {
            this.Values = new int[size];
            for (int i = 0; i < size; i++)
            {
                this.Values[i] = i;
            }
        }

        /// <summary>
        /// Make a permutation from a set of values such that p[i] = x for  the value x at position i.
        /// </summary>
        /// <param name="values">the elements of the permutation</param>
        public Permutation(params int[] values)
        {
            this.Values = values;
        }

        /// <summary>
        /// Construct a permutation from another one by cloning the values.
        /// </summary>
        /// <param name="other">the other permutation</param>
        public Permutation(Permutation other)
        {
            this.Values = (int[])other.Values.Clone();
        }

        public override bool Equals(object other)
        {
            if (this == other) return true;
            if (other == null || GetType() != other.GetType()) return false;

            return Arrays.AreEqual(Values, ((Permutation)other).Values);
        }

        public override int GetHashCode()
        {
            return Arrays.GetHashCode(Values);
        }

        /// <summary>
        /// Check to see if this permutation is the identity permutation.
        /// </summary>
        /// <returns><see langword="true"/> if for all i, p[i] = i</returns>
        public bool IsIdentity()
        {
            for (int i = 0; i < this.Values.Length; i++)
            {
                if (this.Values[i] != i)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The number of elements in the permutation.
        /// </summary>
        public int Count => this.Values.Length;

        /// <summary>
        /// The permutation value
        /// </summary>
        /// <param name="index">Index</param>
        public int this[int index]
        {
            get
            {
                return this.Values[index];
            }
            set
            {
                this.Values[index] = value;
            }
        }

        /// <summary>
        /// All the values as an array.
        /// </summary>
        public int[] Values { get; private set; }

        /// <summary>
        /// Find an r such that <c>this[r] != other[r]</c>.
        /// </summary>
        /// <param name="other">the other permutation to compare with</param>
        /// <returns>the first point at which the two permutations differ</returns>
        public int FirstIndexOfDifference(Permutation other)
        {
            int r = 0;
            while ((r < Values.Length) && Values[r] == other[r])
            {
                r++;
            }
            return r;
        }

        /// <summary>
        /// Get all the elements in the same orbit in the permutation (unsorted).
        /// </summary>
        /// <param name="element">any element in the orbit</param>
        /// <returns>the list of elements reachable in this permutation</returns>
        public IList<int> GetOrbit(int element)
        {
            var orbit = new List<int>();
            orbit.Add(element);
            int i = Values[element];
            while (i != element && orbit.Count < Values.Length)
            {
                orbit.Add(i);
                i = Values[i];
            }
            return orbit;
        }

        /// <summary>
        /// Alter a permutation by setting it to the values in the other permutation.
        /// </summary>
        /// <param name="other">the other permutation to use</param>
        /// <exception cref="ArgumentException"> if the permutations are of different size</exception>
        public void SetTo(Permutation other)
        {
            if (this.Values.Length != other.Values.Length)
                throw new ArgumentException("permutations are different size");

            for (int i = 0; i < this.Values.Length; i++)
            {
                this.Values[i] = other.Values[i];
            }
        }

        /// <summary>
        /// Multiply this permutation by another such that for all i, this[i] = this[other[i]].
        /// </summary>
        /// <param name="other">the other permutation to use</param>
        /// <returns>a new permutation with the result of multiplying the permutations</returns>
        public Permutation Multiply(Permutation other)
        {
            Permutation newPermutation = new Permutation(Values.Length);
            for (int i = 0; i < Values.Length; i++)
            {
                newPermutation.Values[i] = this.Values[other.Values[i]];
            }
            return newPermutation;
        }

        /// <summary>
        /// Invert the permutation, so that for all i : inv[p[i]] = i.
        /// </summary>
        /// <returns>the inverse of this permutation</returns>
        public Permutation Invert()
        {
            Permutation inversion = new Permutation(Values.Length);
            for (int i = 0; i < Values.Length; i++)
            {
                inversion.Values[this.Values[i]] = i;
            }
            return inversion;
        }

        /// <summary>
        /// An easily-readable version of the permutation as a product of cycles.
        /// </summary>
        /// <returns>the cycle form of the permutation as a string</returns>
        public string ToCycleString()
        {
            int n = this.Values.Length;
            bool[] p = new bool[n];
            Arrays.Fill(p, true);

            StringBuilder sb = new StringBuilder();
            int j = 0;
            for (int i = 0; i < n; i++)
            {
                if (p[i])
                {
                    sb.Append('(');
                    sb.Append(i);
                    p[i] = false;
                    j = i;
                    while (p[Values[j]])
                    {
                        sb.Append(", ");
                        j = Values[j];
                        sb.Append(j);
                        p[j] = false;
                    }
                    sb.Append(')');
                }
            }
            return sb.ToString();
        }

        public IEnumerator<int> GetEnumerator()
        {
            return ((IEnumerable<int>)Values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Values.GetEnumerator();
        }
    }
}
