// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Straight-line interpolation between a set of points.
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s <c>osu.Helpers.Interpolators.LinearInterpolator</c>. The interpolator is marked as
    /// <see cref="Ignored"/> because the covered frames already exist verbatim in the replay; it only supplies
    /// derivatives to neighbouring interpolators.
    /// </remarks>
    public class LinearInterpolator : IInterpolator
    {
        protected readonly List<PointWithTime> Points;

        public int StartTime { get; }
        public int EndTime { get; }

        public LinearInterpolator(List<PointWithTime> points)
        {
            if (points.Count < 2)
                throw new ArgumentException("Cannot interpolate with less than 2 points.", nameof(points));

            Points = new List<PointWithTime>(points);
            StartTime = Points[0].Time;
            EndTime = Points[^1].Time;
        }

        public bool Ignored() => true;

        public Vector2 PositionAtTime(int time) => throw new InvalidOperationException();

        public Vector2 DerivativeAtStartTime()
        {
            int duration = Math.Max(Points[1].Time - Points[0].Time, 1);
            return (Points[1].Position - Points[0].Position) / duration;
        }

        public Vector2 DerivativeAtEndTime()
        {
            int duration = Math.Max(Points[^1].Time - Points[^2].Time, 1);
            return (Points[^1].Position - Points[^2].Position) / duration;
        }
    }
}
