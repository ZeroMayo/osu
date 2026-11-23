// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// A cursor position paired with the time it was sampled at.
    /// </summary>
    /// <remarks>Port of osu!auto+'s <c>osu.Helpers.PointWithTime</c>.</remarks>
    public readonly struct PointWithTime : IComparable<PointWithTime>
    {
        public readonly int Time;
        public readonly Vector2 Position;

        public PointWithTime(int time, Vector2 position)
        {
            Time = time;
            Position = position;
        }

        public PointWithTime(OsuReplayFrame frame)
            : this((int)Math.Round(frame.Time), frame.Position)
        {
        }

        public int CompareTo(PointWithTime other) => Time.CompareTo(other.Time);
    }
}
