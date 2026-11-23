// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// A single replay time segment produced by one of the interpolation routines.
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s <c>osu.Helpers.IInterpolator</c>. All times are in milliseconds and, as in the original,
    /// are integral.
    /// </remarks>
    public interface IInterpolator
    {
        /// <summary>
        /// The time at which this interpolator starts producing positions.
        /// </summary>
        int StartTime { get; }

        /// <summary>
        /// The time at which this interpolator stops producing positions.
        /// </summary>
        int EndTime { get; }

        /// <summary>
        /// Whether this interpolator should not be used to generate new replay frames.
        /// For example, a <see cref="LinearInterpolator"/> only exists to provide end derivatives for its
        /// neighbours, since its positions are already present as raw replay frames.
        /// </summary>
        bool Ignored();

        /// <summary>
        /// Retrieves the cursor position at the given time.
        /// </summary>
        /// <param name="time">The time to retrieve the position at. Must be within [<see cref="StartTime"/>, <see cref="EndTime"/>].</param>
        Vector2 PositionAtTime(int time);

        /// <summary>
        /// The derivative (in playfield units per millisecond) at <see cref="StartTime"/>.
        /// </summary>
        Vector2 DerivativeAtStartTime();

        /// <summary>
        /// The derivative (in playfield units per millisecond) at <see cref="EndTime"/>.
        /// </summary>
        Vector2 DerivativeAtEndTime();
    }
}
