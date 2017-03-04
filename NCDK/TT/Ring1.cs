
















// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2015-2017  Kazuya Ujihara

using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace NCDK.Default
{
    [Serializable]
    public class Ring
        : AtomContainer, IRing
    {
        public Ring()
            : base()
        { }

        public Ring(IAtomContainer atomContainer)
            : base(atomContainer)
        { }

        public Ring(
           IEnumerable<IAtom> atoms,
           IEnumerable<IBond> bonds)
           : base(atoms, bonds)
        { }

        public Ring(int ringSize, string elementSymbol)
            : base()
        {
            var prevAtom = new Atom(elementSymbol);
            atoms.Add(prevAtom);
            for (int i = 1; i < ringSize; i++)
            {
                var atom = new Atom(elementSymbol);
                atoms.Add(atom);
                bonds.Add(new Bond(prevAtom, atom, BondOrder.Single));
                prevAtom = atom;
            }
            bonds.Add(new Bond(prevAtom, atoms[0], BondOrder.Single));
        }

        public int RingSize => atoms.Count;

        public IBond GetNextBond(IBond bond, IAtom atom)
            => bonds.Where(n => n.Contains(atom) && n != bond).FirstOrDefault();

        public int BondOrderSum
            => bonds.Where(n => n.Order != BondOrder.Unset).Select(n => n.Order.Numeric).Sum();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Ring(");
            sb.Append(base.ToString());
            sb.Append(')');
            return sb.ToString();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            return base.Clone(map);
        }
    }
}
namespace NCDK.Silent
{
    [Serializable]
    public class Ring
        : AtomContainer, IRing
    {
        public Ring()
            : base()
        { }

        public Ring(IAtomContainer atomContainer)
            : base(atomContainer)
        { }

        public Ring(
           IEnumerable<IAtom> atoms,
           IEnumerable<IBond> bonds)
           : base(atoms, bonds)
        { }

        public Ring(int ringSize, string elementSymbol)
            : base()
        {
            var prevAtom = new Atom(elementSymbol);
            atoms.Add(prevAtom);
            for (int i = 1; i < ringSize; i++)
            {
                var atom = new Atom(elementSymbol);
                atoms.Add(atom);
                bonds.Add(new Bond(prevAtom, atom, BondOrder.Single));
                prevAtom = atom;
            }
            bonds.Add(new Bond(prevAtom, atoms[0], BondOrder.Single));
        }

        public int RingSize => atoms.Count;

        public IBond GetNextBond(IBond bond, IAtom atom)
            => bonds.Where(n => n.Contains(atom) && n != bond).FirstOrDefault();

        public int BondOrderSum
            => bonds.Where(n => n.Order != BondOrder.Unset).Select(n => n.Order.Numeric).Sum();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Ring(");
            sb.Append(base.ToString());
            sb.Append(')');
            return sb.ToString();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            return base.Clone(map);
        }
    }
}
