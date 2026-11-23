// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Interpolates a stream by following the circular arcs implied by consecutive cursor points.
    /// </summary>
    /// <remarks>
    /// Port of osu!auto+'s <c>osu.Helpers.Interpolators.StreamAngularInterpolator</c>. The original's config lookups
    /// are supplied through <see cref="AutoPlusInterpolationOptions"/>.
    /// </remarks>
    public class StreamAngularInterpolator : IInterpolator
    {
        protected readonly List<PointWithTime> Points;
        protected readonly List<Vector2> Derivatives;

        protected readonly AutoPlusInterpolationOptions Options;

        protected int CurrentPointIndex;

        public int StartTime { get; }
        public int EndTime { get; }

        public StreamAngularInterpolator(List<PointWithTime> points, AutoPlusInterpolationOptions options)
        {
            if (points.Count < 2)
                throw new ArgumentException("Cannot interpolate with less than 2 points.", nameof(points));

            Points = new List<PointWithTime>(points);
            Options = options;
            StartTime = Points[0].Time;
            EndTime = Points[^1].Time;
            Derivatives = new List<Vector2>();
            CurrentPointIndex = 0;
            computeDerivatives();
        }

        public bool Ignored() => false;

        public Vector2 PositionAtTime(int time)
        {
            if (time < StartTime || time > EndTime)
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time has to be between StartTime and EndTime.");

            // The original loop condition (`CurrentPointIndex != Points.Count`) could read past the end of the point
            // list when the requested time was exactly EndTime; the bound is `Count - 1` here instead.
            while (CurrentPointIndex < Points.Count - 1 && Points[CurrentPointIndex + 1].Time <= time)
                CurrentPointIndex++;

            while (CurrentPointIndex > 0 && Points[CurrentPointIndex].Time > time)
                CurrentPointIndex--;

            if (Points[CurrentPointIndex].Time == time)
                return Points[CurrentPointIndex].Position;

            float progress = (float)(time - Points[CurrentPointIndex].Time) / (Points[CurrentPointIndex + 1].Time - Points[CurrentPointIndex].Time);

            Vector2 derivative = Derivatives[CurrentPointIndex];
            Vector2 chord = Points[CurrentPointIndex + 1].Position - Points[CurrentPointIndex].Position;
            Vector2 normal = new Vector2(-chord.Y, chord.X);
            normal /= 2f;

            if (Vector2.Dot(derivative, chord) < 0)
                derivative = -derivative;

            if (Vector2.Dot(derivative, normal) < 0)
                normal = -normal;

            Vector2 normalDirection = normal;
            normalDirection.Normalize();

            float cosine = Vector2.Dot(derivative, normalDirection);
            float sineSquared = 1 - cosine * cosine;
            float ratio = sineSquared / cosine;
            float along = normal.Length * ratio;

            if (along > chord.Length)
                along = chord.Length / 2f;

            float across = normal.Length;

            if (ratio != 0)
                across = along / ratio;

            chord.Normalize();

            Vector2 circleCentre = Points[CurrentPointIndex].Position + chord * along + normalDirection * across;

            float distanceToCentre = (circleCentre - Points[CurrentPointIndex].Position).Length;
            float distanceToEnd = (Points[CurrentPointIndex + 1].Position - circleCentre).Length;
            float travelled = progress * (distanceToCentre + distanceToEnd);

            if (travelled <= distanceToCentre)
                return Points[CurrentPointIndex].Position + derivative * travelled;

            return circleCentre + Derivatives[CurrentPointIndex + 1] * (travelled - distanceToCentre);
        }

        public Vector2 DerivativeAtStartTime()
        {
            float distance = (Points[1].Position - Points[0].Position).Length;

            return Derivatives[0] * (distance / 2f) * (float)Options.StreamSpeedFactor / (Points[1].Time - Points[0].Time);
        }

        public Vector2 DerivativeAtEndTime()
        {
            int lastSegment = Points.Count - 2;
            float distance = (Points[lastSegment + 1].Position - Points[lastSegment].Position).Length;

            return Derivatives[^1] * (distance / 2f) * (float)Options.StreamSpeedFactor / (Points[lastSegment + 1].Time - Points[lastSegment].Time);
        }

        private void computeDerivatives()
        {
            Vector2 chord = Points[1].Position - Points[0].Position;
            Vector2 normal = new Vector2(-chord.Y, chord.X);

            if (Points.Count > 2)
            {
                Vector2 firstDirection = normal;
                firstDirection.Normalize();

                Vector2 secondDirection = Points[2].Position - Points[1].Position;
                secondDirection.Normalize();

                if (Vector2.Dot(firstDirection, secondDirection) > 0.99)
                    normal = -normal;
            }

            Vector2 derivative = chord + normal;
            derivative.Normalize();
            Derivatives.Add(derivative);

            for (int i = 1; i < Points.Count; i++)
            {
                chord = Points[i].Position - Points[i - 1].Position;
                normal = new Vector2(-chord.Y, chord.X);
                normal /= 2f;

                if (Vector2.Dot(derivative, chord) < 0)
                    derivative = -derivative;

                if (Vector2.Dot(derivative, normal) < 0)
                    normal = -normal;

                Vector2 normalDirection = normal;
                normalDirection.Normalize();

                float cosine = Vector2.Dot(derivative, normalDirection);
                float sineSquared = 1 - cosine * cosine;
                float ratio = sineSquared / cosine;
                float along = normal.Length * ratio;

                if (along > chord.Length)
                    along = chord.Length / 2f;

                float across = normal.Length;

                if (ratio != 0)
                    across = along / ratio;

                chord.Normalize();

                Vector2 circleCentre = Points[i - 1].Position + chord * along + normalDirection * across;

                derivative = Points[i].Position - circleCentre;
                derivative.Normalize();
                Derivatives.Add(derivative);
            }
        }
    }
}
