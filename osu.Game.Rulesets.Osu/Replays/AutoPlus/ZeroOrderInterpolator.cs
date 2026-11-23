// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// The "00" (zero-order) interpolation mode, used in place of the other interpolators when
    /// <see cref="AutoPlusInterpolationOptions.UseZeroOrderInterpolation"/> is enabled.
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s <c>osu.Helpers.Interpolators._00Interpolator</c>. As in the original, it never
    /// returns a real position and always yields the origin.
    /// </remarks>
    public class ZeroOrderInterpolator : IInterpolator
    {
        protected readonly List<PointWithTime> Points;

        public int StartTime { get; }
        public int EndTime { get; }

        public ZeroOrderInterpolator(List<PointWithTime> points)
        {
            if (points.Count < 2)
                throw new ArgumentException("Cannot interpolate with less than 2 points.", nameof(points));

            Points = new List<PointWithTime>(points);
            StartTime = Points[0].Time;
            EndTime = Points[^1].Time;
        }

        public bool Ignored() => false;

        public Vector2 PositionAtTime(int time) => Vector2.Zero;

        public Vector2 DerivativeAtStartTime() => Vector2.Zero;

        public Vector2 DerivativeAtEndTime() => Vector2.Zero;
    }
}
