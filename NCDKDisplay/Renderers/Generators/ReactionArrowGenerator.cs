/*  Copyright (C) 2009  Stefan Kuhn <shk3@users.sf.net>
 *
 *  Contact: cdk-devel@list.sourceforge.net
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
using NCDK.Renderers.Elements;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using static NCDK.Renderers.Generators.BasicSceneGenerator;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generate the arrow for a reaction.
    /// </summary>
    // @author maclean
    // @cdk.module renderextra
    // @cdk.githash
    public class ReactionArrowGenerator : IGenerator<IReaction>
    {
        /// <inheritdoc/>
        public IRenderingElement Generate(IReaction reaction, RendererModel model)
        {
            var totalBoundsReactants = BoundsCalculator.CalculateBounds(reaction.Reactants);
            var totalBoundsProducts = BoundsCalculator.CalculateBounds(reaction.Products);

            if (totalBoundsReactants == null || totalBoundsProducts == null) return null;

            double separation = model.GetV<double>(typeof(BondLength)) / model.GetV<double>(typeof(Scale));
            var foregroundColor = model.GetV<Color>(typeof(BasicSceneGenerator.ForegroundColor));
            return new ArrowElement(
                new WPF::Point(totalBoundsReactants.Right + separation, totalBoundsReactants.GetCenterY()),
                new WPF::Point(totalBoundsProducts.Left - separation, totalBoundsReactants.GetCenterY()),
                1 / model.GetV<double>(typeof(Scale)), true, foregroundColor);
        }

        /// <inheritdoc/>
        public IList<IGeneratorParameter> Parameters => Array.Empty<IGeneratorParameter>();
    }
}
