// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Fills the gaps between raw autoplay replay frames with interpolated cursor paths.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Port of the interpolation stage of osu!auto+'s <c>osu.GameModes.Play.Rulesets.Osu.RulesetOsu</c>
    /// (<c>InterpSpinners</c>, <c>InterpFollowSliders</c>, <c>InterpCloseFrames</c>, <c>InterpStreams</c>,
    /// <c>InterpStreamsAngular</c>, <c>InterpRest</c>, <c>SliderPositionAtTime</c> and <c>SliderDerivativeAtTime</c>).
    /// </para>
    /// <para>
    /// The methods operate on a time-ordered frame list and append to a list of <see cref="IInterpolator"/>s.
    /// As in the original, the caller is expected to sort the interpolator list by <see cref="IInterpolator.StartTime"/>
    /// (see <see cref="InterpolatorComparer"/>) before calling <see cref="InterpRest"/>, which then fills in
    /// everything that is still uncovered.
    /// </para>
    /// </remarks>
    public static class OsuAutoPlusInterpolator
    {
        /// <summary>
        /// The position at the centre of the playfield, used as the starting point for the very first spline.
        /// </summary>
        private static readonly Vector2 playfield_centre = new Vector2(256, 192);

        /// <summary>
        /// The interpolation pipeline quantises frame times to whole milliseconds, while slider boundaries may be
        /// fractional, so slider lookups tolerate one millisecond of reach beyond the slider's time range.
        /// </summary>
        private const double slider_time_tolerance = 1;

        /// <summary>
        /// Interpolates consecutive spinner frames linearly.
        /// </summary>
        public static void InterpSpinners(List<FrameWithType> frames, List<IInterpolator> interpolators)
        {
            for (int i = 0; i < frames.Count - 1; i++)
            {
                if (IsCovered(frames, interpolators, i) || frames[i].Type != ObjectType.Spin || frames[i + 1].Type != ObjectType.Spin)
                    continue;

                List<PointWithTime> points = new List<PointWithTime>
                {
                    new PointWithTime(frames[i].Frame),
                    new PointWithTime(frames[i + 1].Frame)
                };

                int next;

                for (next = i + 2; next < frames.Count && !IsCovered(frames, interpolators, next - 1) && frames[next].Type == ObjectType.Spin; next++)
                    points.Add(new PointWithTime(frames[next].Frame));

                interpolators.Add(new LinearInterpolator(points));
                i = next - 2;
            }
        }

        /// <summary>
        /// Makes the cursor follow a slider's path for as long as consecutive frames stay on the same slider.
        /// </summary>
        public static void InterpFollowSliders(List<FrameWithType> frames, List<IInterpolator> interpolators, AutoPlusInterpolationOptions options)
        {
            for (int i = 0; i < frames.Count - 1; i++)
            {
                if (IsCovered(frames, interpolators, i) || frames[i].Slider == null || frames[i + 1].Slider != frames[i].Slider)
                    continue;

                int next;

                for (next = i + 1; next < frames.Count - 1 && !IsCovered(frames, interpolators, next) && frames[next + 1].Slider == frames[next].Slider; next++)
                {
                }

                interpolators.Add(new SliderFollowInterpolator(frames[i].Time, frames[next].Time, frames[i].Slider!, options));
                i = next - 1;
            }
        }

        /// <summary>
        /// Interpolates linearly between frames which are either simultaneous or move faster than 5 playfield units per millisecond.
        /// </summary>
        public static void InterpCloseFrames(List<FrameWithType> frames, List<IInterpolator> interpolators)
        {
            double speed(FrameWithType from, FrameWithType to)
            {
                int duration = Math.Max(to.Time - from.Time, 1);
                return (to.Position - from.Position).Length / duration;
            }

            for (int i = 0; i < frames.Count - 1; i++)
            {
                if (IsCovered(frames, interpolators, i) || !(frames[i].Time == frames[i + 1].Time || speed(frames[i], frames[i + 1]) > 5))
                    continue;

                List<PointWithTime> points = new List<PointWithTime>
                {
                    new PointWithTime(frames[i].Frame),
                    new PointWithTime(frames[i + 1].Frame)
                };

                int next;

                for (next = i + 2; next < frames.Count && !IsCovered(frames, interpolators, next - 1) && (frames[next - 1].Time == frames[next].Time || speed(frames[next - 1], frames[next]) > 5); next++)
                    points.Add(new PointWithTime(frames[next].Frame));

                interpolators.Add(new LinearInterpolator(points));
                i = next - 2;
            }
        }

        /// <summary>
        /// Detects equally spaced, sharply angled point sequences and interpolates them along circular arcs.
        /// </summary>
        public static void InterpStreamsAngular(List<FrameWithType> frames, List<IInterpolator> interpolators, AutoPlusInterpolationOptions options)
        {
            const double minimum_distance_squared = 25;
            const double maximum_distance_squared = 8000;
            const double minimum_dot_product = -0.99;
            const double angular_minimum_dot_product = -0.01;
            const double angular_maximum_dot_product = 0.01;

            double distanceSquared(FrameWithType from, FrameWithType to)
                => Math.Pow(to.Position.X - from.Position.X, 2) + Math.Pow(to.Position.Y - from.Position.Y, 2);

            double angleDotProduct(FrameWithType previous, FrameWithType current, FrameWithType next)
            {
                Vector2 incoming = previous.Position - current.Position;
                Vector2 outgoing = next.Position - current.Position;

                incoming.Normalize();
                outgoing.Normalize();

                return Vector2.Dot(incoming, outgoing);
            }

            for (int i = 0; i < frames.Count - 2; i++)
            {
                if (IsCovered(frames, interpolators, i) || IsCovered(frames, interpolators, i + 1)
                                                                 || Math.Abs(frames[i + 2].Time - frames[i + 1].Time - (frames[i + 1].Time - frames[i].Time)) >= 2
                                                                 || !(distanceSquared(frames[i + 2], frames[i + 1]) >= minimum_distance_squared)
                                                                 || !(distanceSquared(frames[i + 2], frames[i + 1]) <= maximum_distance_squared)
                                                                 || !(distanceSquared(frames[i + 1], frames[i]) >= minimum_distance_squared)
                                                                 || !(distanceSquared(frames[i + 1], frames[i]) <= maximum_distance_squared))
                    continue;

                double dot = angleDotProduct(frames[i], frames[i + 1], frames[i + 2]);

                if (dot > minimum_dot_product && (!(dot <= angular_maximum_dot_product) || !(dot >= angular_minimum_dot_product)))
                    continue;

                int interval = frames[i + 1].Time - frames[i].Time;

                List<PointWithTime> points = new List<PointWithTime>
                {
                    new PointWithTime(frames[i].Frame),
                    new PointWithTime(frames[i + 1].Frame),
                    new PointWithTime(frames[i + 2].Frame)
                };

                int next;

                for (next = i + 3; next < frames.Count
                                       && !IsCovered(frames, interpolators, next - 1)
                                       && Math.Abs(frames[next].Time - frames[next - 1].Time - interval) < 2
                                       && distanceSquared(frames[next], frames[next - 1]) >= minimum_distance_squared
                                       && distanceSquared(frames[next], frames[next - 1]) <= maximum_distance_squared;
                     next++)
                {
                    double nextDot = angleDotProduct(frames[next - 2], frames[next - 1], frames[next]);

                    if (nextDot > minimum_dot_product && (!(nextDot <= angular_maximum_dot_product) || !(nextDot >= angular_minimum_dot_product)))
                        break;

                    points.Add(new PointWithTime(frames[next].Frame));
                }

                interpolators.Add(new StreamAngularInterpolator(points, options));
                i = next - 2;
            }
        }

        /// <summary>
        /// Detects equally spaced point sequences and interpolates them along half-circle arcs.
        /// </summary>
        public static void InterpStreams(List<FrameWithType> frames, List<IInterpolator> interpolators, AutoPlusInterpolationOptions options)
        {
            const double minimum_distance_squared = 25;
            const double maximum_distance_squared = 4000;
            const double maximum_dot_product = -0.5;
            const double angular_fallback_dot_product = -0.99;

            double distanceSquared(FrameWithType from, FrameWithType to)
                => Math.Pow(to.Position.X - from.Position.X, 2) + Math.Pow(to.Position.Y - from.Position.Y, 2);

            double angleDotProduct(FrameWithType previous, FrameWithType current, FrameWithType next)
            {
                Vector2 incoming = previous.Position - current.Position;
                Vector2 outgoing = next.Position - current.Position;

                incoming.Normalize();
                outgoing.Normalize();

                return Vector2.Dot(incoming, outgoing);
            }

            for (int i = 0; i < frames.Count - 2; i++)
            {
                if (IsCovered(frames, interpolators, i) || IsCovered(frames, interpolators, i + 1)
                                                                 || Math.Abs(frames[i + 2].Time - frames[i + 1].Time - (frames[i + 1].Time - frames[i].Time)) >= 2
                                                                 || !(distanceSquared(frames[i + 2], frames[i + 1]) >= minimum_distance_squared)
                                                                 || !(distanceSquared(frames[i + 2], frames[i + 1]) <= maximum_distance_squared)
                                                                 || !(distanceSquared(frames[i + 1], frames[i]) >= minimum_distance_squared)
                                                                 || !(distanceSquared(frames[i + 1], frames[i]) <= maximum_distance_squared)
                                                                 || !(angleDotProduct(frames[i], frames[i + 1], frames[i + 2]) <= maximum_dot_product))
                    continue;

                bool useAngular = options.UseAngularStreamInterpolation;

                if (angleDotProduct(frames[i], frames[i + 1], frames[i + 2]) > angular_fallback_dot_product)
                    useAngular = false;

                int interval = frames[i + 1].Time - frames[i].Time;

                List<PointWithTime> points = new List<PointWithTime>
                {
                    new PointWithTime(frames[i].Frame),
                    new PointWithTime(frames[i + 1].Frame),
                    new PointWithTime(frames[i + 2].Frame)
                };

                int next;

                for (next = i + 3; next < frames.Count
                                       && !IsCovered(frames, interpolators, next - 1)
                                       && Math.Abs(frames[next].Time - frames[next - 1].Time - interval) < 2
                                       && distanceSquared(frames[next], frames[next - 1]) >= minimum_distance_squared
                                       && distanceSquared(frames[next], frames[next - 1]) <= maximum_distance_squared
                                       && angleDotProduct(frames[next - 2], frames[next - 1], frames[next]) <= maximum_dot_product;
                     next++)
                {
                    if (angleDotProduct(frames[next - 2], frames[next - 1], frames[next]) > angular_fallback_dot_product)
                        useAngular = false;

                    points.Add(new PointWithTime(frames[next].Frame));
                }

                if (!useAngular)
                    interpolators.Add(new StreamInterpolator(points, options));

                i = next - 2;
            }
        }

        /// <summary>
        /// Fills every remaining gap between frames with a spline (or a straight line, depending on <paramref name="options"/>).
        /// </summary>
        /// <remarks>
        /// <paramref name="interpolators"/> is expected to be sorted by <see cref="IInterpolator.StartTime"/>.
        /// </remarks>
        public static void InterpRest(List<FrameWithType> frames, List<IInterpolator> interpolators, AutoPlusInterpolationOptions options)
        {
            if (frames.Count == 0)
                return;

            int uncoveredIndex = 0;

            for (int i = 0; i < interpolators.Count; i++)
            {
                IInterpolator interpolator = interpolators[i];

                if (interpolator.StartTime > frames[uncoveredIndex].Time)
                {
                    int end = uncoveredIndex;

                    while (end < frames.Count && frames[end].Time < interpolator.StartTime)
                        end++;

                    end = Math.Min(end, frames.Count - 1);

                    List<PointWithTime> points = new List<PointWithTime>();

                    for (int j = uncoveredIndex; j <= end; j++)
                        points.Add(new PointWithTime(frames[j].Frame));

                    // A single point cannot be interpolated; the original would have thrown in this case.
                    if (points.Count < 2)
                        continue;

                    Vector2 derivativeAtStart;

                    if (i == 0)
                    {
                        derivativeAtStart.X = (frames[uncoveredIndex].Position.X - playfield_centre.X) / 1000f;
                        derivativeAtStart.Y = (frames[uncoveredIndex].Position.Y - playfield_centre.Y) / 1000f;
                    }
                    else
                        derivativeAtStart = interpolators[i - 1].DerivativeAtEndTime();

                    if (options.UseZeroOrderInterpolation)
                        interpolators.Insert(i, new ZeroOrderInterpolator(points));
                    else if (options.UseCubicSplineInterpolation)
                        interpolators.Insert(i, new CubicSplineInterpolator(points, derivativeAtStart, interpolator.DerivativeAtStartTime()));
                    else
                        interpolators.Insert(i, new LinearInterpolator(points));

                    i++;
                }

                while (uncoveredIndex != frames.Count - 1 && frames[uncoveredIndex + 1].Time <= interpolator.EndTime)
                    uncoveredIndex++;
            }

            if (frames[^1].Time > frames[uncoveredIndex].Time)
            {
                List<PointWithTime> points = new List<PointWithTime>();

                for (int i = uncoveredIndex; i < frames.Count; i++)
                    points.Add(new PointWithTime(frames[i].Frame));

                if (points.Count < 2)
                    return;

                if (options.UseCubicSplineInterpolation)
                {
                    Vector2 derivativeAtStart = interpolators.Count > 0 ? interpolators[^1].DerivativeAtEndTime() : Vector2.Zero;
                    interpolators.Add(new CubicSplineInterpolator(points, derivativeAtStart, Vector2.Zero));
                }
                else
                    interpolators.Add(new LinearInterpolator(points));
            }
        }

        /// <summary>
        /// Retrieves the position of the slider's follower at the given time.
        /// </summary>
        /// <remarks>
        /// The original walked the movement transformations of the legacy slider follower and returned
        /// <see cref="Vector2.Zero"/> when no transformation covered <paramref name="time"/>.
        /// The lazer equivalent evaluates the slider's <see cref="Slider.Path"/>, with the same fallback.
        /// </remarks>
        public static Vector2 SliderPositionAtTime(Slider slider, double time)
        {
            double duration = slider.Duration;

            if (duration <= 0)
                return Vector2.Zero;

            if (!CoversTime(slider, time))
                return Vector2.Zero;

            double progress = Math.Clamp((time - slider.StartTime) / duration, 0, 1);

            return slider.StackedPositionAt(progress);
        }

        /// <summary>
        /// Retrieves the derivative (in playfield units per millisecond) of the slider's follower at the given time.
        /// </summary>
        /// <remarks>
        /// The original divided the end-to-start vector of the movement transformation covering <paramref name="time"/>
        /// by its duration. The lazer equivalent differentiates the slider's <see cref="Slider.Path"/> with respect to
        /// time, taking repeats into account.
        /// </remarks>
        public static Vector2 SliderDerivativeAtTime(Slider slider, double time)
        {
            double duration = slider.Duration;

            if (duration <= 0)
                return Vector2.Zero;

            if (!CoversTime(slider, time))
                return Vector2.Zero;

            double progress = Math.Clamp((time - slider.StartTime) / duration, 0, 1);

            // Derivative of the path position with respect to the progress along the (possibly repeated) path.
            const double epsilon = 1e-4;

            // A progress of exactly 1 would report a span index past the end, flipping the direction of the final span.
            double sampleProgress = Math.Min(progress, 1 - 1e-6);

            double pathProgress = slider.ProgressAt(sampleProgress);
            double from = Math.Clamp(pathProgress - epsilon, 0, 1);
            double to = Math.Clamp(pathProgress + epsilon, 0, 1);

            Vector2 pathDerivative = (slider.Path.PositionAt(to) - slider.Path.PositionAt(from)) / (float)(to - from);

            // Repeats reverse the direction on every other span.
            float spanDirection = slider.SpanAt(sampleProgress) % 2 == 0 ? 1 : -1;

            return pathDerivative * (float)(spanDirection * slider.SpanCount() / duration);
        }

        /// <summary>
        /// Whether the given (whole millisecond) time lies on or within one millisecond of the slider's time range.
        /// </summary>
        internal static bool CoversTime(Slider slider, double time)
            => time >= slider.StartTime - slider_time_tolerance && time <= slider.EndTime + slider_time_tolerance;

        /// <summary>
        /// Whether the interval between two consecutive frames is already covered by an interpolator.
        /// </summary>
        private static bool IsCovered(List<FrameWithType> frames, List<IInterpolator> interpolators, int indexFrom)
        {
            if (indexFrom < 0 || indexFrom >= frames.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(indexFrom), indexFrom, "indexFrom has to be between 0 and frames.Count - 2.");

            int start = frames[indexFrom].Time;
            int end = frames[indexFrom + 1].Time;

            return interpolators.Exists(interpolator => interpolator.StartTime <= start && interpolator.EndTime >= end);
        }
    }
}
