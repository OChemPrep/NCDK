﻿/* Copyright (C) 2004-2017  The Chemistry Development Kit (CDK) project
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

namespace NCDK.QSAR.Results
{
    public interface IResult
        : IDescriptorResult
    {
    }

    internal static class ResultInstance<T>
    {
        public static readonly Result<T> Value = new Result<T>();
    }

    public static class Result
    {
        public static Result<T> Instance<T>() => ResultInstance<T>.Value;
    }

    public class Result<T>
        : IResult
    {
        public T Value { get; private set; }

        public Result()
            : this(default(T))             
        { }

        public Result(T value)
        {
            Value = value;
        }

        public override string ToString() => Value.ToString();
        public int Length => 1;
    }
}