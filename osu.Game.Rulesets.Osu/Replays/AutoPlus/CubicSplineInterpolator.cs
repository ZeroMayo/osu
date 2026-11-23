// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Interpolates a cursor path through a set of points using a cubic spline with fixed derivatives at both ends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Port of osu!auto+'s <c>osu.Helpers.Interpolators.CubicSplineInterpolator</c>. The original delegates the maths to
    /// the native <c>Spline.dll</c> (<c>spline_interp(1.0, ...)</c> / <c>spline_free(...)</c>), which samples a clamped
    /// cubic spline at a fixed 1ms step and then looks positions up by millisecond.
    /// </para>
    /// <para>
    /// The native dependency is replaced by an equivalent managed implementation: a C2 cubic spline with specified
    /// first derivatives at both end points, sampled once at construction and looked up by millisecond exactly like
    /// the original. Points sharing a timestamp are collapsed (the native library requires strictly increasing
    /// abscissae); the last point of each group wins.
    /// </para>
    /// </remarks>
    public class CubicSplineInterpolator : IInterpolator
    {
        /// <summary>
        /// As in the original, endpoint derivatives longer than this are scaled down to this length.
        /// </summary>
        private const double maximum_endpoint_derivative_length = 5;

        /// <summary>
        /// The sampling step used by the original native call (<c>spline_interp(1.0, ...)</c>), in milliseconds.
        /// </summary>
        private const double sample_step = 1;

        private readonly double[] sampleXs;
        private readonly double[] sampleYs;

        public int StartTime { get; }
        public int EndTime { get; }

        public Vector2 DerivativeAtStart { get; }
        public Vector2 DerivativeAtEnd { get; }

        public CubicSplineInterpolator(List<PointWithTime> points, Vector2 derivativeAtStart, Vector2 derivativeAtEnd)
        {
            if (points.Count < 2)
                throw new ArgumentException("Cannot interpolate with less than 2 points.", nameof(points));

            List<PointWithTime> ordered = collapseDuplicateTimes(points);

            if (ordered.Count < 2)
                throw new ArgumentException("Cannot interpolate with less than 2 distinct times.", nameof(points));

            StartTime = ordered[0].Time;
            EndTime = ordered[^1].Time;

            if (StartTime == EndTime)
                throw new ArgumentException("StartTime must be lower than EndTime.", nameof(points));

            DerivativeAtStart = clampDerivative(derivativeAtStart);
            DerivativeAtEnd = clampDerivative(derivativeAtEnd);

            int knotCount = ordered.Count;

            double[] ts = new double[knotCount];
            double[] xs = new double[knotCount];
            double[] ys = new double[knotCount];

            for (int i = 0; i < knotCount; i++)
            {
                ts[i] = ordered[i].Time;
                xs[i] = ordered[i].Position.X;
                ys[i] = ordered[i].Position.Y;
            }

            int sampleCount = EndTime - StartTime + 1;

            sampleXs = sample(ts, xs, DerivativeAtStart.X, DerivativeAtEnd.X, sampleCount);
            sampleYs = sample(ts, ys, DerivativeAtStart.Y, DerivativeAtEnd.Y, sampleCount);
        }

        public bool Ignored() => false;

        public Vector2 PositionAtTime(int time)
        {
            if (time < StartTime || time > EndTime)
                throw new ArgumentOutOfRangeException(nameof(time), time, "Time has to be between StartTime and EndTime.");

            // Samples are spaced exactly one millisecond apart, starting at StartTime.
            int index = time - StartTime;

            return new Vector2((float)sampleXs[index], (float)sampleYs[index]);
        }

        public Vector2 DerivativeAtStartTime() => DerivativeAtStart;

        public Vector2 DerivativeAtEndTime() => DerivativeAtEnd;

        private static List<PointWithTime> collapseDuplicateTimes(List<PointWithTime> points)
        {
            List<PointWithTime> ordered = new List<PointWithTime>(points.Count);

            foreach (PointWithTime point in points)
            {
                if (ordered.Count > 0 && ordered[^1].Time == point.Time)
                    ordered[^1] = point;
                else
                    ordered.Add(point);
            }

            return ordered;
        }

        private static Vector2 clampDerivative(Vector2 derivative)
        {
            float length = derivative.Length;

            return length > maximum_endpoint_derivative_length
                ? derivative * (float)(maximum_endpoint_derivative_length / length)
                : derivative;
        }

        private static double[] sample(IReadOnlyList<double> ts, IReadOnlyList<double> vs, double startDerivative, double endDerivative, int sampleCount)
        {
            double[] secondDerivatives = solveSecondDerivatives(ts, vs, startDerivative, endDerivative);
            double[] result = new double[sampleCount];

            int segment = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                double t = ts[0] + i * sample_step;

                while (segment < ts.Count - 2 && ts[segment + 1] < t)
                    segment++;

                result[i] = evaluate(ts, vs, secondDerivatives, segment, t);
            }

            return result;
        }

        /// <summary>
        /// Solves for the second derivatives of a cubic spline passing through <paramref name="vs"/> at
        /// <paramref name="ts"/>, with the first derivative fixed to <paramref name="startDerivative"/> and
        /// <paramref name="endDerivative"/> at the respective end points.
        /// </summary>
        private static double[] solveSecondDerivatives(IReadOnlyList<double> ts, IReadOnlyList<double> vs, double startDerivative, double endDerivative)
        {
            int n = ts.Count;

            double[] lower = new double[n];
            double[] diagonal = new double[n];
            double[] upper = new double[n];
            double[] right = new double[n];

            for (int i = 1; i < n - 1; i++)
            {
                double previousInterval = ts[i] - ts[i - 1];
                double nextInterval = ts[i + 1] - ts[i];

                lower[i] = previousInterval;
                diagonal[i] = 2 * (previousInterval + nextInterval);
                upper[i] = nextInterval;
                right[i] = 6 * ((vs[i + 1] - vs[i]) / nextInterval - (vs[i] - vs[i - 1]) / previousInterval);
            }

            double startInterval = ts[1] - ts[0];
            diagonal[0] = 2 * startInterval;
            upper[0] = startInterval;
            right[0] = 6 * ((vs[1] - vs[0]) / startInterval - startDerivative);

            double endInterval = ts[n - 1] - ts[n - 2];
            lower[n - 1] = endInterval;
            diagonal[n - 1] = 2 * endInterval;
            right[n - 1] = 6 * (endDerivative - (vs[n - 1] - vs[n - 2]) / endInterval);

            // Thomas algorithm.
            for (int i = 1; i < n; i++)
            {
                double factor = lower[i] / diagonal[i - 1];
                diagonal[i] -= factor * upper[i - 1];
                right[i] -= factor * right[i - 1];
            }

            double[] secondDerivatives = new double[n];
            secondDerivatives[n - 1] = right[n - 1] / diagonal[n - 1];

            for (int i = n - 2; i >= 0; i--)
                secondDerivatives[i] = (right[i] - upper[i] * secondDerivatives[i + 1]) / diagonal[i];

            return secondDerivatives;
        }

        private static double evaluate(IReadOnlyList<double> ts, IReadOnlyList<double> vs, IReadOnlyList<double> secondDerivatives, int segment, double t)
        {
            double interval = ts[segment + 1] - ts[segment];

            if (interval <= 0)
                return vs[segment];

            double normalizedEnd = (ts[segment + 1] - t) / interval;
            double normalizedStart = (t - ts[segment]) / interval;

            return normalizedEnd * vs[segment]
                   + normalizedStart * vs[segment + 1]
                   + interval * interval / 6
                   * ((normalizedEnd * normalizedEnd * normalizedEnd - normalizedEnd) * secondDerivatives[segment]
                      + (normalizedStart * normalizedStart * normalizedStart - normalizedStart) * secondDerivatives[segment + 1]);
        }
    }
}
