// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Mods
{
    [HeadlessTest]
    public partial class TestSceneAutoPlusPlayback : OsuModTestScene
    {
        protected override bool AllowFail => true;

        [Test]
        public void TestAutoPlusReplayHitsEverything()
        {
            var beatmap = new OsuBeatmap { BeatmapInfo = { Difficulty = new BeatmapDifficulty { SliderMultiplier = 1.4, SliderTickRate = 1, OverallDifficulty = 8 } } };
            beatmap.ControlPointInfo.Add(0, new osu.Game.Beatmaps.ControlPoints.TimingControlPoint { BeatLength = 500 });

            beatmap.HitObjects.Add(new HitCircle { StartTime = 500, Position = new Vector2(180, 120) });
            beatmap.HitObjects.Add(new HitCircle { StartTime = 800, Position = new Vector2(380, 160) });
            beatmap.HitObjects.Add(new Slider
            {
                StartTime = 1100,
                Position = new Vector2(120, 150),
                RepeatCount = 1,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(60, 40))
                })
            });
            beatmap.HitObjects.Add(new Spinner { StartTime = 1700, Duration = 600, Position = new Vector2(256, 192) });

            foreach (var h in beatmap.HitObjects)
                h.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            List<ReplayFrame> frames = new OsuAutoGenerator(beatmap, System.Array.Empty<Mod>())
                .Generate().Frames.ToList();

            CreateModTest(new ModTestData
            {
                Autoplay = false,
                ReplayFrames = frames,
                CreateBeatmap = () => beatmap,
                PassCondition = () => Player.ScoreProcessor.JudgedHits == beatmap.HitObjects.Count && Player.ScoreProcessor.Accuracy.Value == 1
            });
        }
    }
}
