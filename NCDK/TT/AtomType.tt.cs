 


// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/*
 *  Copyright (C) 2001-2013  Christoph Steinbeck <steinbeck@users.sf.net>
 *                           John May <jwmay@users.sourceforge.net>
 *                           Egon Willighagen <egonw@users.sourceforge.net>
 *                           Rajarshi Guha <rajarshi@users.sourceforge.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 */
using System;
using System.Text;

namespace NCDK.Default
{
    /// <summary>
    /// The base class for atom types. Atom types are typically used to describe the
    /// behaviour of an atom of a particular element in different environment like
    /// sp<sup>3</sup>
    /// hybridized carbon C3, etc., in some molecular modelling applications.
    /// </summary>
    // @author       steinbeck
    // @cdk.created  2001-08-08
    // @cdk.githash
    // @cdk.keyword  atom, type 
    [Serializable]
    public class AtomType
        : Isotope, IAtomType
    {
        /// <summary>
        ///  The maximum bond order allowed for this atom type.
        /// </summary>
        internal BondOrder maxBondOrder;

        /// <summary>
        ///  The maximum sum of all bond orders allowed for this atom type.
        /// </summary>
        internal double? bondOrderSum;

        /// <summary>
        /// The covalent radius of this atom type.
        /// </summary>
        internal double? covalentRadius;

        /// <summary>
        ///  The formal charge of the atom with CDKConstants.UNSET as default. Implements RFC #6.
        /// </summary>
        /// <remarks>
        ///  Note that some constructors (<see cref="AtomType(string)"/> and
        /// <see cref="AtomType(string, string)"/> ) will explicitly set this field to 0
        /// </remarks>
        internal int? formalCharge;

        /// <summary>
        /// The hybridization state of this atom with CDKConstants.HYBRIDIZATION_UNSET
        /// as default.
        /// </summary>
        internal Hybridization hybridization;

        /// <summary>
        /// The electron Valency of this atom with CDKConstants.UNSET as default.
        /// </summary>
        internal int? valency;

        /// <summary>
        /// The formal number of neighbours this atom type can have with CDKConstants_UNSET
        /// as default. This includes explicitely and implicitely connected atoms, including
        /// implicit hydrogens.
        /// </summary>
        internal int? formalNeighbourCount;

        internal string atomTypeName;
        internal bool isHydrogenBondAcceptor;
        internal bool isHydrogenBondDonor;
        internal bool isAliphatic;
        internal bool isAromatic;
        internal bool isInRing;
        internal bool isReactiveCenter;

        /// <summary>
        /// Constructor for the AtomType object.
        /// 
        /// Defaults to a zero formal charge. All
        /// other fields are set to <see langword="null"/> or unset.
        /// </summary>
        /// <param name="elementSymbol">Symbol of the atom</param>
        public AtomType(string elementSymbol)
            : base(elementSymbol)
        {
            this.formalCharge = 0;
        }

        /// <summary>
        /// Constructor for the AtomType object. Defaults to a zero formal charge.
        /// </summary>
        /// <param name="identifier">An id for this atom type, like C3 for sp3 carbon</param>
        /// <param name="elementSymbol">The element symbol identifying the element to which this atom type applies</param>
        public AtomType(string identifier, string elementSymbol)
            : base(elementSymbol)
        {
            this.atomTypeName = identifier;
        }

        /// <summary>
        /// Constructs an isotope by copying the symbol, atomic number,
        /// flags, identifier, exact mass, natural abundance and mass
        /// number from the given IIsotope. It does not copy the
        /// listeners and properties. If the element is an instanceof
        /// IAtomType, then the maximum bond order, bond order sum,
        /// van der Waals and covalent radii, formal charge, hybridization,
        /// electron valency, formal neighbour count and atom type name
        /// are copied too.
        /// </summary>
        /// <param name="element">IIsotope to copy information from</param>
        public AtomType(IElement element)
            : base(element)
        {
            var aa = element as IAtomType;
            if (aa != null)
            {
                maxBondOrder = aa.MaxBondOrder;
                bondOrderSum = aa.BondOrderSum;
                covalentRadius = aa.CovalentRadius;
                formalCharge = aa.FormalCharge;
                hybridization = aa.Hybridization;
                valency = aa.Valency;
                formalNeighbourCount = aa.FormalNeighbourCount;
                atomTypeName = aa.AtomTypeName;

                isHydrogenBondAcceptor = aa.IsHydrogenBondAcceptor;
                isHydrogenBondDonor = aa.IsHydrogenBondDonor;
                isAromatic = aa.IsAromatic;
                isInRing = aa.IsInRing;
            }
        }

        /// <summary>
        /// The if attribute of the AtomType object.
        /// </summary>
        public virtual string AtomTypeName
        {
            get { return atomTypeName; }
            set 
            {
                atomTypeName = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The MaxBondOrder attribute of the AtomType object.
        /// </summary>
        public virtual BondOrder MaxBondOrder
        {
            get { return maxBondOrder; }
            set
            {
                maxBondOrder = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The the exact bond order sum attribute of the AtomType object.
        /// </summary>
        public virtual double? BondOrderSum
        {
            get { return bondOrderSum; }
            set 
            {
                bondOrderSum = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The formal charge of this atom.
        /// </summary>
        public virtual int? FormalCharge
        {
            get { return formalCharge; }
            set 
            { 
                formalCharge = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The formal neighbour count of this atom.
        /// </summary>
        public virtual int? FormalNeighbourCount
        {
            get { return formalNeighbourCount; }
            set
            { 
                formalNeighbourCount = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The hybridization of this atom.
        /// </summary>
        public virtual Hybridization Hybridization
        {
            get { return hybridization; }
            set { 
                hybridization = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// Compares a atom type with this atom type.
        /// </summary>
        /// <param name="obj">Object of type AtomType</param>
        /// <returns>true if the atom types are equal</returns>
        public override bool Compare(object obj)
        {
            var o = obj as IAtomType;
            return o != null && base.Compare(obj)
                && AtomTypeName == o.AtomTypeName
                && MaxBondOrder == o.MaxBondOrder
                && BondOrderSum == o.BondOrderSum;
        }

        /// <summary>
        /// The covalent radius for this AtomType.
        /// </summary>
        public virtual double? CovalentRadius
        {
            get { return covalentRadius; }
            set
            { 
                covalentRadius = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The the exact electron valency of the AtomType object.
        /// </summary>
        public virtual int? Valency
        {
            get { return valency; }
            set 
            {
                valency = value; 
                NotifyChanged();
            }
        }

        public virtual bool IsHydrogenBondAcceptor
        {
            get { return isHydrogenBondAcceptor; }
            set
            {
                isHydrogenBondAcceptor = value; 
                NotifyChanged();
            }
        }

        public virtual bool IsHydrogenBondDonor
        {
            get { return isHydrogenBondDonor; }
            set
            {
                isHydrogenBondDonor = value; 
                NotifyChanged();
            }
        }

        public virtual bool IsAliphatic
        {
            get { return isAliphatic; }
            set
            { 
                isAliphatic = value; 
                NotifyChanged();
            }
        }

        public virtual bool IsAromatic
        {
            get { return isAromatic; }
            set
            {
                isAromatic = value; 
                NotifyChanged();
            }
        }

        public virtual bool IsInRing
        {
            get { return isInRing; }
            set 
            {
                isInRing = value; 
                NotifyChanged();
            }
        }

        public virtual bool IsReactiveCenter
        {
            get { return isReactiveCenter; }
            set 
            {
                isReactiveCenter = value; 
                NotifyChanged();
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AtomType(").Append(GetHashCode());
            if (AtomTypeName != null)
                sb.Append(", N:").Append(AtomTypeName);
            if (MaxBondOrder != BondOrder.Unset)
                sb.Append(", MBO:").Append(MaxBondOrder);
            if (BondOrderSum != null)
                sb.Append(", BOS:").Append(BondOrderSum);
            if (FormalCharge != null)
                sb.Append(", FC:").Append(FormalCharge);
            if (Hybridization != Hybridization.Unset)
                sb.Append(", H:").Append(Hybridization);
            if (FormalNeighbourCount != null)
                sb.Append(", NC:").Append(FormalNeighbourCount);
            if (CovalentRadius != null)
                sb.Append(", CR:").Append(CovalentRadius);
            if (Valency != null)
                sb.Append(", EV:").Append(Valency);
            sb.Append(", ").Append(base.ToString());
            sb.Append(')');
            return sb.ToString();
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// The base class for atom types. Atom types are typically used to describe the
    /// behaviour of an atom of a particular element in different environment like
    /// sp<sup>3</sup>
    /// hybridized carbon C3, etc., in some molecular modelling applications.
    /// </summary>
    // @author       steinbeck
    // @cdk.created  2001-08-08
    // @cdk.githash
    // @cdk.keyword  atom, type 
    [Serializable]
    public class AtomType
        : Isotope, IAtomType
    {
        /// <summary>
        ///  The maximum bond order allowed for this atom type.
        /// </summary>
        internal BondOrder maxBondOrder;

        /// <summary>
        ///  The maximum sum of all bond orders allowed for this atom type.
        /// </summary>
        internal double? bondOrderSum;

        /// <summary>
        /// The covalent radius of this atom type.
        /// </summary>
        internal double? covalentRadius;

        /// <summary>
        ///  The formal charge of the atom with CDKConstants.UNSET as default. Implements RFC #6.
        /// </summary>
        /// <remarks>
        ///  Note that some constructors (<see cref="AtomType(string)"/> and
        /// <see cref="AtomType(string, string)"/> ) will explicitly set this field to 0
        /// </remarks>
        internal int? formalCharge;

        /// <summary>
        /// The hybridization state of this atom with CDKConstants.HYBRIDIZATION_UNSET
        /// as default.
        /// </summary>
        internal Hybridization hybridization;

        /// <summary>
        /// The electron Valency of this atom with CDKConstants.UNSET as default.
        /// </summary>
        internal int? valency;

        /// <summary>
        /// The formal number of neighbours this atom type can have with CDKConstants_UNSET
        /// as default. This includes explicitely and implicitely connected atoms, including
        /// implicit hydrogens.
        /// </summary>
        internal int? formalNeighbourCount;

        internal string atomTypeName;
        internal bool isHydrogenBondAcceptor;
        internal bool isHydrogenBondDonor;
        internal bool isAliphatic;
        internal bool isAromatic;
        internal bool isInRing;
        internal bool isReactiveCenter;

        /// <summary>
        /// Constructor for the AtomType object.
        /// 
        /// Defaults to a zero formal charge. All
        /// other fields are set to <see langword="null"/> or unset.
        /// </summary>
        /// <param name="elementSymbol">Symbol of the atom</param>
        public AtomType(string elementSymbol)
            : base(elementSymbol)
        {
            this.formalCharge = 0;
        }

        /// <summary>
        /// Constructor for the AtomType object. Defaults to a zero formal charge.
        /// </summary>
        /// <param name="identifier">An id for this atom type, like C3 for sp3 carbon</param>
        /// <param name="elementSymbol">The element symbol identifying the element to which this atom type applies</param>
        public AtomType(string identifier, string elementSymbol)
            : base(elementSymbol)
        {
            this.atomTypeName = identifier;
        }

        /// <summary>
        /// Constructs an isotope by copying the symbol, atomic number,
        /// flags, identifier, exact mass, natural abundance and mass
        /// number from the given IIsotope. It does not copy the
        /// listeners and properties. If the element is an instanceof
        /// IAtomType, then the maximum bond order, bond order sum,
        /// van der Waals and covalent radii, formal charge, hybridization,
        /// electron valency, formal neighbour count and atom type name
        /// are copied too.
        /// </summary>
        /// <param name="element">IIsotope to copy information from</param>
        public AtomType(IElement element)
            : base(element)
        {
            var aa = element as IAtomType;
            if (aa != null)
            {
                maxBondOrder = aa.MaxBondOrder;
                bondOrderSum = aa.BondOrderSum;
                covalentRadius = aa.CovalentRadius;
                formalCharge = aa.FormalCharge;
                hybridization = aa.Hybridization;
                valency = aa.Valency;
                formalNeighbourCount = aa.FormalNeighbourCount;
                atomTypeName = aa.AtomTypeName;

                isHydrogenBondAcceptor = aa.IsHydrogenBondAcceptor;
                isHydrogenBondDonor = aa.IsHydrogenBondDonor;
                isAromatic = aa.IsAromatic;
                isInRing = aa.IsInRing;
            }
        }

        /// <summary>
        /// The if attribute of the AtomType object.
        /// </summary>
        public virtual string AtomTypeName
        {
            get { return atomTypeName; }
            set 
            {
                atomTypeName = value; 
            }
        }

        /// <summary>
        /// The MaxBondOrder attribute of the AtomType object.
        /// </summary>
        public virtual BondOrder MaxBondOrder
        {
            get { return maxBondOrder; }
            set
            {
                maxBondOrder = value; 
            }
        }

        /// <summary>
        /// The the exact bond order sum attribute of the AtomType object.
        /// </summary>
        public virtual double? BondOrderSum
        {
            get { return bondOrderSum; }
            set 
            {
                bondOrderSum = value; 
            }
        }

        /// <summary>
        /// The formal charge of this atom.
        /// </summary>
        public virtual int? FormalCharge
        {
            get { return formalCharge; }
            set 
            { 
                formalCharge = value; 
            }
        }

        /// <summary>
        /// The formal neighbour count of this atom.
        /// </summary>
        public virtual int? FormalNeighbourCount
        {
            get { return formalNeighbourCount; }
            set
            { 
                formalNeighbourCount = value; 
            }
        }

        /// <summary>
        /// The hybridization of this atom.
        /// </summary>
        public virtual Hybridization Hybridization
        {
            get { return hybridization; }
            set { 
                hybridization = value; 
            }
        }

        /// <summary>
        /// Compares a atom type with this atom type.
        /// </summary>
        /// <param name="obj">Object of type AtomType</param>
        /// <returns>true if the atom types are equal</returns>
        public override bool Compare(object obj)
        {
            var o = obj as IAtomType;
            return o != null && base.Compare(obj)
                && AtomTypeName == o.AtomTypeName
                && MaxBondOrder == o.MaxBondOrder
                && BondOrderSum == o.BondOrderSum;
        }

        /// <summary>
        /// The covalent radius for this AtomType.
        /// </summary>
        public virtual double? CovalentRadius
        {
            get { return covalentRadius; }
            set
            { 
                covalentRadius = value; 
            }
        }

        /// <summary>
        /// The the exact electron valency of the AtomType object.
        /// </summary>
        public virtual int? Valency
        {
            get { return valency; }
            set 
            {
                valency = value; 
            }
        }

        public virtual bool IsHydrogenBondAcceptor
        {
            get { return isHydrogenBondAcceptor; }
            set
            {
                isHydrogenBondAcceptor = value; 
            }
        }

        public virtual bool IsHydrogenBondDonor
        {
            get { return isHydrogenBondDonor; }
            set
            {
                isHydrogenBondDonor = value; 
            }
        }

        public virtual bool IsAliphatic
        {
            get { return isAliphatic; }
            set
            { 
                isAliphatic = value; 
            }
        }

        public virtual bool IsAromatic
        {
            get { return isAromatic; }
            set
            {
                isAromatic = value; 
            }
        }

        public virtual bool IsInRing
        {
            get { return isInRing; }
            set 
            {
                isInRing = value; 
            }
        }

        public virtual bool IsReactiveCenter
        {
            get { return isReactiveCenter; }
            set 
            {
                isReactiveCenter = value; 
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AtomType(").Append(GetHashCode());
            if (AtomTypeName != null)
                sb.Append(", N:").Append(AtomTypeName);
            if (MaxBondOrder != BondOrder.Unset)
                sb.Append(", MBO:").Append(MaxBondOrder);
            if (BondOrderSum != null)
                sb.Append(", BOS:").Append(BondOrderSum);
            if (FormalCharge != null)
                sb.Append(", FC:").Append(FormalCharge);
            if (Hybridization != Hybridization.Unset)
                sb.Append(", H:").Append(Hybridization);
            if (FormalNeighbourCount != null)
                sb.Append(", NC:").Append(FormalNeighbourCount);
            if (CovalentRadius != null)
                sb.Append(", CR:").Append(CovalentRadius);
            if (Valency != null)
                sb.Append(", EV:").Append(Valency);
            sb.Append(", ").Append(base.ToString());
            sb.Append(')');
            return sb.ToString();
        }
    }
}
