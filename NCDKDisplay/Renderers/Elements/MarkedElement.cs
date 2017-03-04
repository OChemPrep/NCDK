/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// A marked element adds meta-data (id and tags) to a CDK rendering
    /// element (or group of elements). The id should be unique per depiction.
    /// The primary use case it to be able to set the 'id' and 'class'
    /// attributes in SVG.
    /// </summary>
    /// <example>
    /// To set the mol, atom, or bond id set a string property to <see cref="ID_KEY"/>.
    /// Similarly, the <see cref="CLASS_KEY"/> can be used to set the classes.
    /// <code>
    /// IAtomContainer mol;
    /// atom.SetProperty(MarkedElement.ID_KEY, "my_atm_id");
    /// atom.SetProperty(MarkedElement.CLASS_KEY, "h_donor");
    /// atom.SetProperty(MarkedElement.CLASS_KEY, "h_acceptor");
    /// </code>
    /// </example>
    public sealed class MarkedElement : IRenderingElement
    {
        public const string ID_KEY = nameof(MarkedElement) + "_ID";
        public const string CLASS_KEY = nameof(MarkedElement) + "_CLS";

        readonly IRenderingElement elem;
        private readonly IList<string> classes = new List<string>(5);

        private MarkedElement(IRenderingElement elem)
        {
            this.elem = elem;
        }

        /// <summary>
        /// The identifier of the tagged element.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Add a cls to the element.
        /// </summary>
        /// <param name="cls">a cls</param>
        private void AggClass(string cls)
        {
            if (cls != null)
                this.classes.Add(cls);
        }

        /// <summary>
        /// Access the classes of the element.
        /// </summary>
        /// <returns>id, empty if none</returns>
        public IList<string> GetClasses()
        {
            return new ReadOnlyCollection<string>(classes);
        }

        public void Accept(IRenderingVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <summary>
        /// Access the element of which the id and classes apply.
        /// </summary>
        /// <returns>rendering element</returns>
        public IRenderingElement Element()
        {
            return elem;
        }

        /// <summary>
        /// Markup a rendering element with the specified classes.
        /// </summary>
        /// <param name="elem">rendering element</param>
        /// <param name="classes">classes</param>
        /// <returns>the marked element</returns>
        public static MarkedElement Markup(IRenderingElement elem, params string[] classes)
        {
            Debug.Assert(elem != null);
            MarkedElement tagElem = new MarkedElement(elem);
            foreach (var cls in classes)
                tagElem.AggClass(cls);
            return tagElem;
        }

        private static MarkedElement MarkupChemObj(IRenderingElement elem, IChemObject chemObj)
        {
            Debug.Assert(elem != null);
            MarkedElement tagElem = new MarkedElement(elem);
            if (chemObj != null)
            {
                tagElem.Id = chemObj.GetProperty<string>(ID_KEY);
                tagElem.AggClass(chemObj.GetProperty<string>(CLASS_KEY));
            }
            return tagElem;
        }

        /// <summary>
        /// Markup a molecule with the class 'mol' and optionally the ids/classes
        /// from it's properties.
        /// </summary>
        /// <param name="elem">rendering element</param>
        /// <param name="mol">molecule</param>
        /// <returns>the marked element</returns>
        public static MarkedElement MarkupMol(IRenderingElement elem, IAtomContainer mol)
        {
            Debug.Assert(elem != null);
            MarkedElement tagElem = MarkupChemObj(elem, mol);
            tagElem.AggClass("mol");
            return tagElem;
        }

        /// <summary>
        /// Markup a atom with the class 'atom' and optionally the ids/classes
        /// from it's properties.
        /// </summary>
        /// <param name="elem">rendering element</param>
        /// <param name="atom">atom</param>
        /// <returns>the marked element</returns>
        public static MarkedElement MarkupAtom(IRenderingElement elem, IAtom atom)
        {
            if (elem == null)
                return null;
            MarkedElement tagElem = MarkupChemObj(elem, atom);
            tagElem.AggClass("atom");
            return tagElem;
        }

        /// <summary>
        /// Markup a bond with the class 'bond' and optionally the ids/classes
        /// from it's properties.
        /// </summary>
        /// <param name="elem">rendering element</param>
        /// <param name="bond">bond</param>
        /// <returns>the marked element</returns>
        public static MarkedElement MarkupBond(IRenderingElement elem, IBond bond)
        {
            Debug.Assert(elem != null);
            MarkedElement tagElem = MarkupChemObj(elem, bond);
            tagElem.AggClass("bond");
            return tagElem;
        }
    }
}
