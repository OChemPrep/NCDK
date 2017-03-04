/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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
using System.Windows.Media;

namespace NCDK.Renderers.Colors
{
    /// <summary>
    /// Interface to a class for coloring atoms.
    /// </summary>
    // @cdk.module  render
    // @cdk.githash   
    public interface IAtomColorer
    {
        /// <summary>
        /// Returns the color for a certain atom type.
        /// </summary>
        /// <param name="atom">the atom whose color is desired</param>
        /// <returns>the color of the specified atom</returns>
        Color GetAtomColor(IAtom atom);

        /// <summary>
        /// Returns the color for a certain atom type, and uses the given default color if it fails to identify the atom type.
        /// </summary>
        /// <param name="atom">the atom in question</param>
        /// <param name="defaultColor">the color to use if the atom type of this atom cannot be identified</param>
        /// <returns>the color of the specified atom</returns>
        Color GetAtomColor(IAtom atom, Color defaultColor);
    }
}
