// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Interpolates a stream by tracing half-circles between consecutive points.
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s <c>osu.Helpers.Interpolators.StreamInterpolator</c>. The original's config lookups are
    /// supplied through <see cref="AutoPlusInterpolationOptions"/>.
    /// </remarks>
    public class StreamInterpolator : IInterpolator
    {
        protected readonly List<PointWithTime> Points;
        protected readonly AutoPlusInterpolationOptions Options;

        protected int CurrentPointIndex;

        public int StartTime { get; }
        public int EndTime { get; }

        public StreamInterpolator(List<PointWithTime> points, AutoPlusInterpolationOptions options)
        {
            if (points.Count < 2)
                throw new ArgumentException("Cannot interpolate with less than 2 points.", nameof(points));

            Points = new List<PointWithTime>(points);
            Options = options;
            StartTime = Points[0].Time;
            EndTime = Points[^1].Time;
            CurrentPointIndex = 0;
        }

        public bool Ignored() => false;

        public Vector2 PositionAtTime(int time)
        {
            if (time < StartTime || time > EndTime)
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time has to be between StartTime and EndTime.");

            double borderAngle = borderAngleRadians();

            // The original loop condition (`CurrentPointIndex != Points.Count`) could read past the end of the point
            // list when the requested time was exactly EndTime; the bound is `Count - 1` here instead.
            while (CurrentPointIndex < Points.Count - 1 && Points[CurrentPointIndex + 1].Time <= time)
                CurrentPointIndex++;

            while (CurrentPointIndex > 0 && Points[CurrentPointIndex].Time > time)
                CurrentPointIndex--;

            if (Points[CurrentPointIndex].Time == time)
                return Points[CurrentPointIndex].Position;

            float progress = (float)(time - Points[CurrentPointIndex].Time) / (Points[CurrentPointIndex + 1].Time - Points[CurrentPointIndex].Time);

            bool clockwise = CurrentPointIndex % 2 == 0;

            double angle = progress * (Math.PI - 2 * borderAngle) + borderAngle;

            Vector2 chord = Points[CurrentPointIndex + 1].Position - Points[CurrentPointIndex].Position;
            Vector2 normal = new Vector2(-chord.Y, chord.X);
            normal.Normalize();

            double radius = chord.Length / 2f / Math.Cos(borderAngle);

            double along = (Math.Cos(angle) - Math.Cos(borderAngle)) / (Math.Cos(Math.PI - borderAngle) - Math.Cos(borderAngle));
            double across = (Math.Sin(angle) - Math.Sin(borderAngle)) * radius * (clockwise ? 1 : -1);

            return Points[CurrentPointIndex].Position + chord * (float)along + normal * (float)across;
        }

        public Vector2 DerivativeAtStartTime()
        {
            double borderAngle = borderAngleRadians();

            const bool clockwise = true;

            Vector2 chord = Points[1].Position - Points[0].Position;
            float chordLength = chord.Length;

            chord.Normalize();

            Vector2 normal = new Vector2(-chord.Y, chord.X);

            double along = (clockwise ? 1 : -1) * Math.Sin(borderAngle);
            double across = (clockwise ? 1 : -1) * Math.Cos(borderAngle);

            Vector2 direction = chord * (float)along + normal * (float)across;
            direction.Normalize();

            return direction * (chordLength / 2f) * (float)Options.StreamSpeedFactor / (Points[1].Time - Points[0].Time);
        }

        public Vector2 DerivativeAtEndTime()
        {
            double borderAngle = borderAngleRadians();

            int lastSegment = Points.Count - 2;
            bool clockwise = lastSegment % 2 == 0;

            Vector2 chord = Points[lastSegment + 1].Position - Points[lastSegment].Position;
            float chordLength = chord.Length;

            chord.Normalize();

            Vector2 normal = new Vector2(-chord.Y, chord.X);

            double along = (clockwise ? 1 : -1) * Math.Sin(Math.PI - borderAngle);
            double across = (clockwise ? 1 : -1) * Math.Cos(Math.PI - borderAngle);

            Vector2 direction = -chord * (float)along + normal * (float)across;
            direction.Normalize();

            return direction * (chordLength / 2f) * (float)Options.StreamSpeedFactor / (Points[lastSegment + 1].Time - Points[lastSegment].Time);
        }

        private double borderAngleRadians() => Math.Min(80, Math.Max(0, Options.StreamHalfCircleBorderAngle)) * Math.PI / 180;
    }
}
