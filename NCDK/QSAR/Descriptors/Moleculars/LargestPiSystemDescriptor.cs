/*  Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */
using NCDK.Aromaticities;
using NCDK.QSAR.Result;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Class that returns the number of atoms in the largest pi system.
    /// <p/>
    /// <p>This descriptor uses these parameters:
    /// <table border="1">
    /// <tr>
    /// <td>Name</td>
    /// <td>Default</td>
    /// <td>Description</td>
    /// </tr>
    /// <tr>
    /// <td>checkAromaticity</td>
    /// <td>false</td>
    /// <td>True is the aromaticity has to be checked</td>
    /// </tr>
    /// </table>
    /// <p/>
    /// Returns a single value named <i>nAtomPi</i>
    ///
    // @author chhoppe from EUROSCREEN
    // @cdk.created 2006-1-03
    // @cdk.module qsarmolecular
    // @cdk.githash
    // @cdk.set qsar-descriptors
    // @cdk.dictref qsar-descriptors:largestPiSystem
    /// </summary>
    public class LargestPiSystemDescriptor : AbstractMolecularDescriptor, IMolecularDescriptor
    {
        private bool checkAromaticity = false;
        private static readonly string[] NAMES = { "nAtomP" };

        /// <summary>
        /// Constructor for the LargestPiSystemDescriptor object.
        /// </summary>
        public LargestPiSystemDescriptor() { }

        /// <inheritdoc/> 
        public override IImplementationSpecification Specification => _Specification;
        private static DescriptorSpecification _Specification { get; } =
         new DescriptorSpecification(
                "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#largestPiSystem",
                typeof(LargestPiSystemDescriptor).FullName,
                "The Chemistry Development Kit");

        /// <summary>
        /// The parameters attribute of the LargestPiSystemDescriptor object.
        /// <para>
        /// This descriptor takes one parameter, which should be bool to indicate whether
        /// aromaticity has been checked (TRUE) or not (FALSE).</para>
        /// </summary>
        /// <exception cref="CDKException">if more than one parameter or a non-bool parameter is specified</exception>
        public override object[] Parameters
        {
            set
            {
                if (value.Length > 1)
                {
                    throw new CDKException("LargestPiSystemDescriptor only expects one parameter");
                }
                if (!(value[0] is bool))
                {
                    throw new CDKException("The first parameter must be of type bool");
                }
                // ok, all should be fine
                checkAromaticity = (bool)value[0];
            }
            get
            {
                // return the parameters as used for the descriptor calculation
                return new object[] { checkAromaticity };
            }
        }

        public override string[] DescriptorNames => NAMES;

        private DescriptorValue GetDummyDescriptorValue(Exception e)
        {
            return new DescriptorValue(_Specification, ParameterNames, Parameters, new IntegerResult(0), DescriptorNames, e);
        }

        /// <summary>
        /// Calculate the count of atoms of the largest pi system in the supplied <see cref="IAtomContainer"/>.
        /// <p/>
        /// <p>The method require one parameter:
        /// <ol>
        /// <li>if checkAromaticity is true, the method check the aromaticity,
        /// <li>if false, means that the aromaticity has already been checked
        /// </ol>
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> for which this descriptor is to be calculated</param>
        /// <returns>the number of atoms in the largest pi system of this AtomContainer</returns>
        /// <seealso cref="Parameters"/>
        public override DescriptorValue Calculate(IAtomContainer container)
        {
            bool[] originalFlag4 = new bool[container.Atoms.Count];
            for (int i = 0; i < originalFlag4.Length; i++)
            {
                originalFlag4[i] = container.Atoms[i].IsVisited;
            }
            if (checkAromaticity)
            {
                try
                {
                    AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                    Aromaticity.CDKLegacy.Apply(container);
                }
                catch (CDKException e)
                {
                    return GetDummyDescriptorValue(e);
                }
            }
            int largestPiSystemAtomsCount = 0;
            //Set all VisitedFlags to False
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                container.Atoms[i].IsVisited = false;
            }
            //Debug.WriteLine("Set all atoms to Visited False");
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                //Possible pi System double bond or triple bond, charge, N or O (free electron pair)
                //Debug.WriteLine("atom:"+i+" maxBondOrder:"+container.GetMaximumBondOrder(atoms[i])+" Aromatic:"+atoms[i].IsAromatic+" FormalCharge:"+atoms[i].FormalCharge+" Charge:"+atoms[i].Charge+" Flag:"+atoms[i].IsVisited);
                if ((container.GetMaximumBondOrder(container.Atoms[i]) != BondOrder.Single
                        || Math.Abs(container.Atoms[i].FormalCharge.Value) >= 1
                        || container.Atoms[i].IsAromatic
                        || container.Atoms[i].Symbol.Equals("N") || container.Atoms[i].Symbol.Equals("O"))
                        & !container.Atoms[i].IsVisited)
                {
                    //Debug.WriteLine("...... -> Accepted");
                    var startSphere = new List<IAtom>();
                    var path = new List<IAtom>();
                    startSphere.Add(container.Atoms[i]);
                    try
                    {
                        BreadthFirstSearch(container, startSphere, path);
                    }
                    catch (CDKException e)
                    {
                        return GetDummyDescriptorValue(e);
                    }
                    if (path.Count > largestPiSystemAtomsCount)
                    {
                        largestPiSystemAtomsCount = path.Count;
                    }
                }

            }
            // restore original flag values
            for (int i = 0; i < originalFlag4.Length; i++)
            {
                container.Atoms[i].IsVisited = originalFlag4[i];
            }

            return new DescriptorValue(_Specification, ParameterNames, Parameters, new IntegerResult(
                    largestPiSystemAtomsCount), DescriptorNames);
        }

        /// <summary>
        /// Returns the specific type of the DescriptorResult object.
        /// <p/>
        /// The return value from this method really indicates what type of result will
        /// be obtained from the <see cref="DescriptorValue"/> object. Note that the same result
        /// can be achieved by interrogating the <see cref="DescriptorValue"/> object; this method
        /// allows you to do the same thing, without actually calculating the descriptor.
        /// </summary>
        public override IDescriptorResult DescriptorResultType { get; } = new IntegerResult(1);

        /// <summary>
        /// Performs a breadthFirstSearch in an AtomContainer starting with a
        /// particular sphere, which usually consists of one start atom, and searches
        /// for a pi system.
        ///
        /// <param name="container">The AtomContainer to</param>
        ///                  be searched
        /// <param name="sphere">A sphere of atoms to</param>
        ///                  start the search with
        /// <param name="path">An array list which stores the atoms belonging to the pi system</param>
        // @throws CDKException
        ///          Description of the
        ///          Exception
        /// </summary>
        private void BreadthFirstSearch(IAtomContainer container, List<IAtom> sphere, List<IAtom> path)
        {
            IAtom nextAtom;
            List<IAtom> newSphere = new List<IAtom>();
            //Debug.WriteLine("Start of breadthFirstSearch");
            foreach (var atom in sphere)
            {
                //Debug.WriteLine("BreadthFirstSearch around atom " + (atomNr + 1));
                var bonds = container.GetConnectedBonds(atom);
                foreach (var bond in bonds)
                {
                    nextAtom = ((IBond)bond).GetConnectedAtom(atom);
                    if ((container.GetMaximumBondOrder(nextAtom) != BondOrder.Single
                            || Math.Abs(nextAtom.FormalCharge.Value) >= 1 || nextAtom.IsAromatic
                            || nextAtom.Symbol.Equals("N") || nextAtom.Symbol.Equals("O"))
                            & !nextAtom.IsVisited)
                    {
                        //Debug.WriteLine("BDS> AtomNr:"+container.Atoms.IndexOf(nextAtom)+" maxBondOrder:"+container.GetMaximumBondOrder(nextAtom)+" Aromatic:"+nextAtom.IsAromatic+" FormalCharge:"+nextAtom.FormalCharge+" Charge:"+nextAtom.Charge+" Flag:"+nextAtom.IsVisited);
                        path.Add(nextAtom);
                        //Debug.WriteLine("BreadthFirstSearch is meeting new atom " + (nextAtomNr + 1));
                        nextAtom.IsVisited = true;
                        if (container.GetConnectedBonds(nextAtom).Count() > 1)
                        {
                            newSphere.Add(nextAtom);
                        }
                    }
                    else
                    {
                        nextAtom.IsVisited = true;
                    }
                }
            }
            if (newSphere.Count > 0)
            {
                BreadthFirstSearch(container, newSphere, path);
            }
        }

        /// <summary>
        /// The parameterNames attribute of the LargestPiSystemDescriptor object.
        /// </summary>
        /// <returns>The parameterNames value</returns>
        public override string[] ParameterNames { get; } = new string[] { "checkAromaticity" };

        /// <summary>
        /// Gets the parameterType attribute of the LargestPiSystemDescriptor object.
        /// </summary>
        /// <param name="name">Description of the Parameter</param>
        /// <returns>An Object of class equal to that of the parameter being requested</returns>
        public override object GetParameterType(string name)
        {
            if ("checkAromaticity".Equals(name)) return false;
            return null;
        }
    }
}
