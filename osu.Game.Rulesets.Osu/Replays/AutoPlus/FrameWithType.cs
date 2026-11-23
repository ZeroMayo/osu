// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// The kind of object a replay frame belongs to.
    /// </summary>
    /// <remarks>Port of osu!auto+'s <c>osu.GameModes.Play.Rulesets.Osu.ObjectType</c>.</remarks>
    public enum ObjectType
    {
        Click,
        Drag,
        Spin
    }

    /// <summary>
    /// A replay frame tagged with the kind of object it belongs to, and the slider it follows (if any).
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s nested <c>FrameWithType</c> struct. The original's <c>bReplayFrame</c> is represented by
    /// lazer's <see cref="OsuReplayFrame"/>; the original's integral frame time is exposed through <see cref="Time"/>.
    /// </remarks>
    public readonly struct FrameWithType
    {
        public readonly OsuReplayFrame Frame;
        public readonly ObjectType Type;
        public readonly Slider? Slider;

        public FrameWithType(OsuReplayFrame frame, ObjectType type, Slider? slider = null)
        {
            Frame = frame;
            Type = type;
            Slider = slider;
        }

        /// <summary>
        /// The frame's time in milliseconds.
        /// </summary>
        public int Time => (int)Math.Round(Frame.Time);

        /// <summary>
        /// The frame's cursor position.
        /// </summary>
        public Vector2 Position => Frame.Position;
    }
}
