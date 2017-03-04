/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    // @cdk.module standard
    // @cdk.githash
    ///
    /// <seealso cref="ChemModelManipulator"/>
    /// </summary>
    public class MoleculeSetManipulator
    {

        public static int GetAtomCount(IAtomContainerSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetAtomCount(set);
        }

        public static int GetBondCount(IAtomContainerSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetBondCount(set);
        }

        public static void RemoveAtomAndConnectedElectronContainers(IAtomContainerSet<IAtomContainer> set, IAtom atom)
        {
            AtomContainerSetManipulator.RemoveAtomAndConnectedElectronContainers(set, atom);
        }

        public static void RemoveElectronContainer(IAtomContainerSet<IAtomContainer> set, IElectronContainer electrons)
        {
            AtomContainerSetManipulator.RemoveElectronContainer(set, electrons);
        }

        /// <summary>
        /// Returns all the AtomContainer's of a MoleculeSet.
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <returns>a list containing individual IAtomContainer's</returns>
        /// </summary>
        public static IEnumerable<IAtomContainer> GetAllAtomContainers(IAtomContainerSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetAllAtomContainers(set);
        }

        /// <summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <seealso cref="AtomContainerSetManipulator"/>
        /// <returns>The total charge on the collection of molecules</returns>
        /// </summary>
        public static double GetTotalCharge(IAtomContainerSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetTotalCharge(set);
        }

        /// <summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <seealso cref="AtomContainerSetManipulator"/>
        /// <returns>The total formal charge on the collection of molecules</returns>
        /// </summary>
        public static double GetTotalFormalCharge(IAtomContainerSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetTotalFormalCharge(set);
        }

        /// <summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <seealso cref="AtomContainerSetManipulator"/>
        /// <returns>the total implicit hydrogen count on the collection of molecules</returns>
        /// </summary>
        public static int GetTotalHydrogenCount(IAtomContainerSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetTotalHydrogenCount(set);
        }

        public static IEnumerable<string> GetAllIDs(IAtomContainerSet<IAtomContainer> set)
        {
            // the ID is set in AtomContainerSetManipulator.GetAllIDs()
            foreach (var id in AtomContainerSetManipulator.GetAllIDs(set))
                yield return id;
            yield break;
        }

        public static void SetAtomProperties(IAtomContainerSet<IAtomContainer> set, string propKey, object propVal)
        {
            AtomContainerSetManipulator.SetAtomProperties(set, propKey, propVal);
        }

        public static IAtomContainer GetRelevantAtomContainer(IAtomContainerSet<IAtomContainer> moleculeSet, IAtom atom)
        {
            return AtomContainerSetManipulator.GetRelevantAtomContainer(moleculeSet, atom);
        }

        public static IAtomContainer GetRelevantAtomContainer(IAtomContainerSet<IAtomContainer> moleculeSet, IBond bond)
        {
            return AtomContainerSetManipulator.GetRelevantAtomContainer(moleculeSet, bond);
        }

        public static IEnumerable<IChemObject> GetAllChemObjects(IAtomContainerSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetAllChemObjects(set);
        }
    }
}
