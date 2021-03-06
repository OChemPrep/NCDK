/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCDK.Tools.Diff.Tree
{
    /// <summary>
    /// <see cref="IDifference"/> between two <see cref="IChemObject"/>s.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    // @cdk.githash
    public class ChemObjectDifference
            : AbstractDifferenceList, IDifferenceList
    {

        private string name;

        public ChemObjectDifference(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Returns a <see cref="string"/> representation for this <see cref="IDifference"/>.
        ///
        /// <returns>a <see cref="string"/></returns>
        /// </summary>

        public override string ToString()
        {
            if (differences.Count() == 0) return "";

            StringBuilder diffBuffer = new StringBuilder();
            diffBuffer.Append(this.name).Append('{');
            IEnumerable<IDifference> children = GetChildren();
            bool isFirst = true;
            foreach (var child in children)
            {
                if (!isFirst)
                {
                    diffBuffer.Append(", ");
                    isFirst = false;
                }
                diffBuffer.Append(child.ToString());
            }
            diffBuffer.Append('}');

            return diffBuffer.ToString();
        }
    }
}
