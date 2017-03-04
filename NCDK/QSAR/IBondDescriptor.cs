/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
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
namespace NCDK.QSAR
{
    /// <summary>
    /// Classes that implement this interface are QSAR descriptor calculators.
    /// </summary>
    // @cdk.module qsar
    // @cdk.githash
    public interface IBondDescriptor : IDescriptor
    {
        /// <summary>
        /// Calculates the descriptor value for the given IBond.
        /// </summary>
        /// <param name="bond">A <see cref="IBond"/> for which this descriptor should be calculated</param>
        /// <param name="atomContainer">The molecule containing the bond</param>
        /// <returns>An object of <see cref="DescriptorValue"/> that contain the calculated value as well as specification details</returns>
        DescriptorValue Calculate(IBond bond, IAtomContainer atomContainer);
    }
}
