// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Follows a slider's path for the duration of the slider.
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s <c>osu.Helpers.Interpolators.SliderFollowInterpolator</c>. The original reads the movement
    /// transformations of the legacy slider follower; the lazer equivalent is the slider's <see cref="Slider.Path"/>,
    /// which is evaluated at the given time.
    /// </remarks>
    public class SliderFollowInterpolator : IInterpolator
    {
        protected readonly Slider Slider;
        protected readonly AutoPlusInterpolationOptions Options;

        public int StartTime { get; }
        public int EndTime { get; }

        public SliderFollowInterpolator(int startTime, int endTime, Slider slider, AutoPlusInterpolationOptions options)
        {
            StartTime = startTime;
            EndTime = endTime;
            Slider = slider;
            Options = options;

            if (!OsuAutoPlusInterpolator.CoversTime(slider, StartTime) || !OsuAutoPlusInterpolator.CoversTime(slider, EndTime))
                throw new ArgumentException("Can't interpolate outside of the slider time.", nameof(slider));
        }

        public bool Ignored() => false;

        public Vector2 PositionAtTime(int time)
        {
            if (time < StartTime || time > EndTime)
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time has to be between StartTime and EndTime.");

            return OsuAutoPlusInterpolator.SliderPositionAtTime(Slider, time);
        }

        public Vector2 DerivativeAtStartTime() => derivativeAtTime(StartTime);

        public Vector2 DerivativeAtEndTime() => derivativeAtTime(EndTime);

        private Vector2 derivativeAtTime(double time) => OsuAutoPlusInterpolator.SliderDerivativeAtTime(Slider, time) * (float)Options.SliderSpeedFactor;
    }
}
