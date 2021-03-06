/* Copyright (C) 2006-2007  Sam Adams <sea36@users.sf.net>
 *                    2009  Jonathan Alvarsson <jonalv@users.sf.net>
 *                    2010  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.NInChI;
using System;
using System.Collections.Generic;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Factory providing access to <see cref="InChIGenerator"/> and <see cref="InChIToStructure"/>.
    /// See those classes for examples of use. These methods make use of the
    /// JNI-InChI library.
    /// </summary>
    /// <example>
    /// The <see cref="InChIGeneratorFactory"/> is a singleton class, which means that there
    /// exists only one instance of the class. An instance of this class is obtained
    /// with:
    /// <code>
    /// InChIGeneratorFactory factory = InChIGeneratorFactory.Instance;
    /// </code>
    /// </example>
    /// <remarks>
    /// InChI/Structure interconversion is implemented in this way so that we can
    /// check whether or not the native code required is available. If the native
    /// code cannot be loaded during the first call to <see cref="Instance"/>
    /// method (when the instance is created) a <see cref="CDKException"/> will be thrown. The
    /// most common problem is that the native code is not in the * the correct
    /// location. Java searches the locations in the PATH environmental
    /// variable, under Windows, and LD_LIBRARY_PATH under Linux, so the JNI-InChI
    /// native libraries must be in one of these locations. If the JNI-InChI jar file
    /// is being used and either the current working directory, or '.' are contained
    /// in PATH of LD_LIBRARY_PATH then the native code should be placed
    /// automatically. If the native files are in the correct location but fail to
    /// load, then they may need to be recompiled for your system. See:
    /// <list type="bullet">
    /// <item>http://sourceforge.net/projects/jni-inchi</item>
    /// <item>http://www.iupac.org/inchi/</item>
    /// </list>
    /// </remarks>
    // @author Sam Adams
    // @cdk.module inchi
    // @cdk.githash
    public class InChIGeneratorFactory
    {
        /// <summary>
        /// Gives the one <see cref="InChIGeneratorFactory"/> instance, 
        /// if needed also creates it.
        /// </summary>
        public static InChIGeneratorFactory Instance { get; } = new InChIGeneratorFactory();

        /// <summary>
        /// If the CDK aromaticity flag should be ignored and the bonds treated solely as single and double bonds.
        /// </summary>
        /// <remarks>
        /// Sets whether aromatic bonds should be treated as single and double bonds for the InChI generation.The bond type
        /// INCHI_BOND_TYPE.Altern is considered special in contrast to single, double, and triple bonds,
        /// and is not bulletproof. If the molecule has clearly defined single and double bonds,
        /// the option can be used to force the class not to use the alternating bond type.
        /// http://www.inchi-trust.org/fileadmin/user_upload/html/inchifaq/inchi-faq.html#16.3
        /// </remarks>
        [Obsolete("\"the use of aromatic bonds is strongly discouraged\" - InChI FAQ, the InChI will fail for many compounds if ignore aromatic bonds is not enabled and the compound have aromatic")]
        public bool IgnoreAromaticBonds { get; set; } = true;

        /// <summary>
        /// Gets an Standard InChI generator for a <see cref="IAtomContainer"/>. AuxInfo is not
        /// generated by this method, please use <see cref="GetInChIGenerator(IAtomContainer, IList{INCHI_OPTION})"/>
        /// with no options specified if you would like to generate AuxInfo.
        /// </summary>
        /// <param name="container">AtomContainer to generate InChI for.</param>
        /// <returns>the InChI generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public InChIGenerator GetInChIGenerator(IAtomContainer container)
        {
            return (new InChIGenerator(container, IgnoreAromaticBonds));
        }

        /// <summary>
        /// Gets InChI generator for CDK IAtomContainer.
        /// </summary>
        /// <param name="container">AtomContainer to generate InChI for.</param>
        /// <param name="options">string of options for InChI generation.</param>
        /// <returns>the InChI generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public InChIGenerator GetInChIGenerator(IAtomContainer container, string options)
        {
            return (new InChIGenerator(container, options, IgnoreAromaticBonds));
        }

        /// <summary>
        /// Gets InChI generator for CDK IAtomContainer.
        /// </summary>
        /// <param name="container">AtomContainer to generate InChI for.</param>
        /// <param name="options">List of options (net.sf.jniinchi.INCHI_OPTION) for InChI generation.</param>
        /// <returns>the InChI generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public InChIGenerator GetInChIGenerator(IAtomContainer container, IList<INCHI_OPTION> options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return (new InChIGenerator(container, options, IgnoreAromaticBonds));
        }

        /// <summary>
        /// Gets structure generator for an InChI string.
        /// </summary>
        /// <param name="inchi">InChI to generate structure from.</param>
        /// <param name="builder">the builder to use</param>
        /// <returns>the InChI structure generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public InChIToStructure GetInChIToStructure(string inchi, IChemObjectBuilder builder)
        {
            return (new InChIToStructure(inchi, builder));
        }

        /// <summary>
        /// Gets structure generator for an InChI string.
        /// </summary>
        /// <param name="inchi">InChI to generate structure from.</param>
        /// <param name="builder">the builder to employ</param>
        /// <param name="options">string of options for structure generation.</param>
        /// <returns>the InChI structure generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public InChIToStructure GetInChIToStructure(string inchi, IChemObjectBuilder builder, string options)
        {
            return (new InChIToStructure(inchi, builder, options));
        }

        /// <summary>
        /// Gets structure generator for an InChI string.
        /// </summary>
        /// <param name="inchi">InChI to generate structure from.</param>
        /// <param name="options">List of options (net.sf.jniinchi.INCHI_OPTION) for structure generation.</param>
        /// <param name="builder">the builder to employ</param>
        /// <returns>the InChI structure generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public InChIToStructure GetInChIToStructure(string inchi, IChemObjectBuilder builder, IList<string> options)
        {
            return (new InChIToStructure(inchi, builder, options));
        }
    }
}
