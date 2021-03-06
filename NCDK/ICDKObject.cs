/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
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
using System;
using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// The base class for all data objects in this CDK.
    /// </summary>
    // @author        egonw
    // @cdk.module    interfaces
    // @cdk.githash
    public interface ICDKObject
        : ICloneable
    {
        /// <summary>
        /// <see cref="IChemObjectBuilder"/> for the data classes that extend this class.
        /// </summary>
        /// <value>The <see cref="IChemObjectBuilder"/> matching this <see cref="ICDKObject"/></value>
        IChemObjectBuilder Builder { get; }

        /// <summary>
        /// A deep clone of this object.
        /// </summary>
        /// <param name="map">A map of the original atoms/bonds to the cloned atoms/bonds.</param>
        /// <returns>Object the clone of this object.</returns>
        ICDKObject Clone(CDKObjectMap map);
    }

    /// <summary>
    /// A mapping of the original atoms/bonds to the cloned atoms/bonds.
    /// </summary>
    public class CDKObjectMap
    {
        IDictionary<IAtom, IAtom> atomMap;
        IDictionary<IBond, IBond> bondMap;

        public CDKObjectMap()
        { }

        /// <summary>
        /// A map of <see cref="IAtom"/>s. 
        /// </summary>
        public IDictionary<IAtom, IAtom> AtomMap
        {
            get
            {
                if (atomMap == null)
                    atomMap = new Dictionary<IAtom, IAtom>();
                return atomMap;
            }
        }

        /// <summary>
        /// A map of <see cref="IBond"/>s. 
        /// </summary>
        public IDictionary<IBond, IBond> BondMap
        {
            get
            {
                if (bondMap == null)
                    bondMap = new Dictionary<IBond, IBond>();
                return bondMap;
            }
        }
    }
}
