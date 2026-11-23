// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Orders interpolators by their start time.
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s <c>osu.Helpers.InterpolatorComparer</c>, required before calling
    /// <see cref="OsuAutoPlusInterpolator.InterpRest"/>.
    /// </remarks>
    public class InterpolatorComparer : IComparer<IInterpolator>
    {
        public int Compare(IInterpolator? x, IInterpolator? y)
        {
            if (x == null)
                return y == null ? 0 : -1;

            if (y == null)
                return 1;

            return x.StartTime.CompareTo(y.StartTime);
        }
    }
}
