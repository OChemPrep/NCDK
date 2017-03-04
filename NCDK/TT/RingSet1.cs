
















// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2015-2017  Kazuya Ujihara

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NCDK.Default
{
    [Serializable]
    public class RingSet
        : AtomContainerSet<IRing>,  IRingSet, ICloneable
    {
        public RingSet()
        {
        }

        public IEnumerable<IRing> GetRings(IAtom atom) => this.Where(n => n.Contains(atom));
        public IEnumerable<IRing> GetRings(IBond bond) => this.Where(n => n.Contains(bond));

        public IEnumerable<IRing> GetConnectedRings(IRing ring)
        {
            IRingSet connectedRings = ring.Builder.CreateRingSet();
            foreach (var atom in ring.Atoms)
                foreach (var tempRing in this)
                    if (tempRing != ring
                      && !connectedRings.Contains(tempRing)
                      && tempRing.Contains(atom))
                    {
                        connectedRings.Add(tempRing);
                    }
            return connectedRings;
        }

        public void Add(IRingSet ringSet)
        {
            foreach (var ring in ringSet)
                if (!Contains(ring))
                    Add(ring);
        }

        public bool Contains(IAtom atom)
            => this.Any(n => n.Contains(atom));

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("RingSet(");
            sb.Append(GetHashCode());
            if (this.Count > 0)
            {
                sb.Append(", R=").Append(this.Count).Append(", ");
                foreach (var possibleRing in this)
                {
                    sb.Append(possibleRing.ToString());
                    sb.Append(", ");
                }
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}
namespace NCDK.Silent
{
    [Serializable]
    public class RingSet
        : AtomContainerSet<IRing>,  IRingSet, ICloneable
    {
        public RingSet()
        {
        }

        public IEnumerable<IRing> GetRings(IAtom atom) => this.Where(n => n.Contains(atom));
        public IEnumerable<IRing> GetRings(IBond bond) => this.Where(n => n.Contains(bond));

        public IEnumerable<IRing> GetConnectedRings(IRing ring)
        {
            IRingSet connectedRings = ring.Builder.CreateRingSet();
            foreach (var atom in ring.Atoms)
                foreach (var tempRing in this)
                    if (tempRing != ring
                      && !connectedRings.Contains(tempRing)
                      && tempRing.Contains(atom))
                    {
                        connectedRings.Add(tempRing);
                    }
            return connectedRings;
        }

        public void Add(IRingSet ringSet)
        {
            foreach (var ring in ringSet)
                if (!Contains(ring))
                    Add(ring);
        }

        public bool Contains(IAtom atom)
            => this.Any(n => n.Contains(atom));

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("RingSet(");
            sb.Append(GetHashCode());
            if (this.Count > 0)
            {
                sb.Append(", R=").Append(this.Count).Append(", ");
                foreach (var possibleRing in this)
                {
                    sb.Append(possibleRing.ToString());
                    sb.Append(", ");
                }
            }
            sb.Append(')');
            return sb.ToString();
        }
    }
}
