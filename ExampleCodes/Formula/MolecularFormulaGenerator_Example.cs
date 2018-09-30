﻿using NCDK.Config;

namespace NCDK.Formula
{
    public class MolecularFormulaGenerator_Example
    {
        public void Main()
        {
            #region
            IsotopeFactory ifac = BODRIsotopeFactory.Instance;
             IIsotope c = ifac.GetMajorIsotope("C");
             IIsotope h = ifac.GetMajorIsotope("H");
             IIsotope n = ifac.GetMajorIsotope("N");
             IIsotope o = ifac.GetMajorIsotope("O");
             IIsotope p = ifac.GetMajorIsotope("P");
             IIsotope s = ifac.GetMajorIsotope("S");
             
             MolecularFormulaRange mfRange = new MolecularFormulaRange();
             mfRange.AddIsotope(c, 0, 50);
             mfRange.AddIsotope(h, 0, 100);
             mfRange.AddIsotope(o, 0, 50);
             mfRange.AddIsotope(n, 0, 50);
             mfRange.AddIsotope(p, 0, 10);
             mfRange.AddIsotope(s, 0, 10);

            var builder = Silent.ChemObjectBuilder.Instance;
            double minMass = 133.003;
            double maxMass = 133.005;
            MolecularFormulaGenerator mfg = new MolecularFormulaGenerator(builder, minMass, maxMass, mfRange);
            IMolecularFormulaSet mfSet = mfg.GetAllFormulas();
            #endregion
        }
    }
}