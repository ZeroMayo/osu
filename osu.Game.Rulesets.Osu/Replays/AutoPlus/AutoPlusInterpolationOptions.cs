// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Osu.Replays.AutoPlus
{
    /// <summary>
    /// Settings used by the ported osu!auto+ interpolation routines.
    /// </summary>
    /// <remarks>
    /// Mirrors the osu!auto+ <c>ConfigManager</c> entries, including their default values:
    /// <c>Auto+UseSplineInterp</c>, <c>Auto+00Interp</c>, <c>Auto+UseAngularStreamInterp</c>,
    /// <c>Auto+SliderSpeedFactor</c>, <c>Auto+StreamSpeedFactor</c> and <c>Auto+StreamHalfCircleBorderAngle</c>.
    /// </remarks>
    public class AutoPlusInterpolationOptions
    {
        /// <summary>
        /// Whether cubic spline interpolation (see <see cref="CubicSplineInterpolator"/>) should be used for the
        /// parts of the replay which are not covered by a more specific interpolator.
        /// Corresponds to <c>Auto+UseSplineInterp</c>.
        /// </summary>
        public bool UseCubicSplineInterpolation { get; set; } = true;

        /// <summary>
        /// Whether the "00" (zero-order) interpolation mode should be used instead of the alternatives.
        /// Corresponds to <c>Auto+00Interp</c>.
        /// </summary>
        public bool UseZeroOrderInterpolation { get; set; } = false;

        /// <summary>
        /// Whether angular stream interpolation may be used.
        /// Corresponds to <c>Auto+UseAngularStreamInterp</c>.
        /// </summary>
        public bool UseAngularStreamInterpolation { get; set; } = true;

        /// <summary>
        /// Multiplier applied to derivatives taken from a slider's path.
        /// Corresponds to <c>Auto+SliderSpeedFactor</c>.
        /// </summary>
        public double SliderSpeedFactor { get; set; } = 4;

        /// <summary>
        /// Multiplier applied to derivatives taken from a stream's point sequence.
        /// Corresponds to <c>Auto+StreamSpeedFactor</c>.
        /// </summary>
        public double StreamSpeedFactor { get; set; } = 4;

        /// <summary>
        /// The angle (in degrees) of the arc drawn between two stream points, clamped to [0, 80] by consumers.
        /// Corresponds to <c>Auto+StreamHalfCircleBorderAngle</c>.
        /// </summary>
        public double StreamHalfCircleBorderAngle { get; set; } = 30;
    }
}
