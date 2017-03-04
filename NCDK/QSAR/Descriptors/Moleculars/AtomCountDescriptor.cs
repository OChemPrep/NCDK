/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.QSAR.Result;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// IDescriptor based on the number of atoms of a certain element type.
    ///
    /// It is
    /// possible to use the wild card symbol '*' as element type to get the count of
    /// all atoms.
    /// <p>This descriptor uses these parameters:
    /// <table border="1">
    ///   <tr>
    ///     <td>Name</td>
    ///     <td>Default</td>
    ///     <td>Description</td>
    ///   </tr>
    ///   <tr>
    ///     <td>elementName</td>
    ///     <td>*</td>
    ///     <td>Symbol of the element we want to count</td>
    ///   </tr>
    /// </table>
    ///
    /// Returns a single value with name <i>nX</i> where <i>X</i> is the atomic symbol.  If *
    /// is specified then the name is <i>nAtom</i>
    ///
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsarmolecular
    // @cdk.githash
    // @cdk.set     qsar-descriptors
    // @cdk.dictref qsar-descriptors:atomCount
    /// </summary>
    public class AtomCountDescriptor : AbstractMolecularDescriptor, IMolecularDescriptor
    {
        private string elementName = "*";

        /// <summary>
        ///  Constructor for the AtomCountDescriptor object.
        /// </summary>
        public AtomCountDescriptor()
        {
            elementName = "*";
        }

        /// <summary>
        /// A map which specifies which descriptor
        /// is implemented by this class.
        ///
        /// These fields are used in the map:
        /// <ul>
        /// <li>Specification-Reference: refers to an entry in a unique dictionary
        /// <li>Implementation-Title: anything
        /// <li>Implementation-Identifier: a unique identifier for this version of
        ///  this class
        /// <li>Implementation-Vendor: CDK, JOELib, or anything else
        /// </ul>
        /// </summary>
        public override IImplementationSpecification Specification => _Specification;
        private static DescriptorSpecification _Specification { get; } =
            new DescriptorSpecification(
                "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#atomCount",
                typeof(AtomCountDescriptor).FullName,
                "The Chemistry Development Kit");

        /// <summary>
        ///  Sets the parameters attribute of the AtomCountDescriptor object.
        /// </summary>
        /// <exception cref="CDKException">if the number of parameters is greater than 1 or else the parameter is not of type string</exception>
        public override object[] Parameters
        {
            set
            {
                if (value.Length > 1)
                {
                    throw new CDKException("AtomCount only expects one parameter");
                }
                if (!(value[0] is string))
                {
                    throw new CDKException("The parameter must be of type string");
                }
                elementName = (string)value[0];
            }
            get
            {
                // return the parameters as used for the descriptor calculation
                return new object[] { elementName };
            }
        }

        public override string[] DescriptorNames
        {
            get
            {
                string name = "n";
                if (elementName.Equals("*"))
                    name = "nAtom";
                else
                    name += elementName;
                return new string[] { name };
            }
        }

        /// <summary>
        ///  This method calculate the number of atoms of a given type in an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="container">The atom container for which this descriptor is to be calculated</param>
        /// <returns>Number of atoms of a certain type is returned.</returns>
        // it could be interesting to accept as elementName a SMARTS atom, to get the frequency of this atom
        // this could be useful for other descriptors like polar surface area...
        public override DescriptorValue Calculate(IAtomContainer container)
        {
            int atomCount = 0;

            if (container == null)
            {
                return new DescriptorValue(_Specification, ParameterNames, Parameters,
                    new IntegerResult(0), DescriptorNames,
                    new CDKException("The supplied AtomContainer was NULL"));
            }

            if (container.Atoms.Count == 0)
            {
                return new DescriptorValue(_Specification, ParameterNames, Parameters,
                    new IntegerResult(0), DescriptorNames, new CDKException(
                        "The supplied AtomContainer did not have any atoms"));
            }

            if (elementName.Equals("*"))
            {
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    // we assume that UNSET is equivalent to 0 implicit H's
                    var hcount = container.Atoms[i].ImplicitHydrogenCount;
                    if (hcount != null) atomCount += hcount.Value;
                }
                atomCount += container.Atoms.Count;
            }
            else if (elementName.Equals("H"))
            {
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    if (container.Atoms[i].Symbol.Equals(elementName))
                    {
                        atomCount += 1;
                    }
                    else
                    {
                        // we assume that UNSET is equivalent to 0 implicit H's
                        var hcount = container.Atoms[i].ImplicitHydrogenCount;
                        if (hcount != null) atomCount += hcount.Value;
                    }
                }
            }
            else
            {
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    if (container.Atoms[i].Symbol.Equals(elementName))
                    {
                        atomCount += 1;
                    }
                }
            }

            return new DescriptorValue(_Specification, ParameterNames, Parameters, new IntegerResult(
                    atomCount), DescriptorNames);
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
        ///  The parameterNames attribute of the AtomCountDescriptor object.
        /// </summary>
        public override string[] ParameterNames { get; } = new string[] { "elementName" };

        /// <summary>
        ///  Gets the parameterType attribute of the AtomCountDescriptor object.
        /// </summary>
        /// <param name="name">Description of the Parameter</param>
        /// <returns>An Object whose class is that of the parameter requested</returns>
        public override object GetParameterType(string name) => "";
    }
}
