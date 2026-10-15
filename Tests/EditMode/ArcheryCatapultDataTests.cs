using System.IO;
using Luban;
using NUnit.Framework;

namespace LOP.MasterData.Tests
{
    /// <summary>
    /// 투석기 사냥(course_kind 3) 배포 데이터 검사. 값의 뜻은 LOP-Shared(<c>ArcheryCatapultSettings</c>)에 있지만
    /// MasterData 패키지는 Shared를 참조하지 않으므로(클·서 격리) 필요한 상수를 여기 베껴 둔다.
    /// </summary>
    public class ArcheryCatapultDataTests
    {
        private const string StreamingAssetsRelative =
            "Packages/com.baegames.lop.masterdata.server/Runtime.Generated/StreamingAssets/MasterData";
        private const float TickSeconds = 0.02f;
        //  원본: LOP.ArcheryAimSystem.FullDrawSeconds = 0.5f
        private const float FullDrawSeconds = 0.5f;

        private static Tables LoadTables()
        {
            string dir = Path.GetFullPath(StreamingAssetsRelative);
            Assert.IsTrue(Directory.Exists(dir), "StreamingAssets 폴더를 찾지 못했다: " + dir);
            return new Tables(name => new ByteBuf(File.ReadAllBytes(Path.Combine(dir, name + ".bytes"))));
        }

        [Test]
        public void 투석기_맵_13의_리듬_값이_spec과_같다()
        {
            var c = LoadTables().TbArcheryConfig.GetOrDefault(13);
            Assert.IsNotNull(c, "TbArcheryConfig 13이 없다");
            const string hint = " — 엑셀 칸 순서나 값이 바뀌었을 수 있다";
            Assert.AreEqual(3, c.CourseKind, "CourseKind" + hint);
            Assert.AreEqual(3, c.RoundCount, "RoundCount" + hint);
            Assert.AreEqual(80, c.RoundIntroTicks, "RoundIntroTicks(1.6초)" + hint);
            Assert.AreEqual(1250, c.RoundHuntTicks, "RoundHuntTicks(25초)" + hint);
            Assert.AreEqual(110, c.RoundResultTicks, "RoundResultTicks(2.2초)" + hint);
            Assert.AreEqual(160, c.VolleyPeriodTicks, "VolleyPeriodTicks(3.2초)" + hint);
            Assert.AreEqual(35, c.VolleyCueTicks, "VolleyCueTicks(0.7초)" + hint);
            Assert.AreEqual(150, c.VolleyQuietTicks, "VolleyQuietTicks(3초)" + hint);
            Assert.AreEqual(130, c.TargetLifeTicks, "TargetLifeTicks(2.6초)" + hint);
            Assert.AreEqual(0.35f, c.TrapChance, 1e-5f, "TrapChance" + hint);
            Assert.AreEqual(16f, c.SpawnRadius, "SpawnRadius(가로 ±16 m)" + hint);
            Assert.AreEqual(0, c.ArrowsPerStand, "화살통 없음" + hint);
        }

        [Test]
        public void 투석기_맵은_활쏘기_모드에_한_발_승부와_같은_씬이다()
        {
            var t = LoadTables();
            var map = t.TbMap.GetOrDefault(13);
            Assert.IsNotNull(map);
            Assert.AreEqual("archery_catapult", map.Code);
            Assert.AreEqual(5, map.GameModeId);
            Assert.AreEqual(t.TbMap.GetOrDefault(7).ScenePath, map.ScenePath);
        }

        [Test]
        public void 투석기_표적은_거리대와_속도대가_있고_한_틱에_반지름보다_적게_간다()
        {
            int count = 0;
            foreach (var k in LoadTables().TbArcheryTarget.DataList)
            {
                if (k.DistanceMaxM <= 0f) { continue; }   // 투석기에 안 나오는 종류(RangeFace)
                count++;
                Assert.GreaterOrEqual(k.DistanceMinM, 30f, $"{k.Code}: 사대에서 30 m 이상(spec §3.2)");
                Assert.LessOrEqual(k.DistanceMinM, k.DistanceMaxM, k.Code);
                Assert.Greater(k.DriftMinMps, 0f, k.Code);
                Assert.LessOrEqual(k.DriftMinMps, k.DriftMaxMps, k.Code);
                Assert.Less(k.DriftMaxMps * TickSeconds, k.Radius, $"{k.Code}: 판정 뚫림");
                Assert.AreEqual(0, k.Shape, $"{k.Code}: 투석기 표적은 공(판정 구)이다");
                Assert.AreEqual(k.IsTrap, k.StunTicks > 0, $"{k.Code}: 사격 금지는 쏘면 안 되는 것만");
            }
            Assert.AreEqual(6, count, "축제 표적 6종");
        }

        [Test]
        public void 표적_수명이_만작과_가장_먼_비행보다_길다()
        {
            var t = LoadTables();
            var c = t.TbArcheryConfig.GetOrDefault(13);
            float farthest = 0f;
            foreach (var k in t.TbArcheryTarget.DataList) { farthest = System.Math.Max(farthest, k.DistanceMaxM); }
            float flight = farthest / c.ArrowMaxSpeedMps;
            Assert.Greater(c.TargetLifeTicks * TickSeconds, FullDrawSeconds + flight);
        }

        [Test]
        public void 무리는_라운드마다_일곱이다()
        {
            var c = LoadTables().TbArcheryConfig.GetOrDefault(13);
            int perRound = (c.RoundHuntTicks - c.VolleyQuietTicks + c.VolleyPeriodTicks - 1) / c.VolleyPeriodTicks;
            Assert.AreEqual(7, perRound);
            int lastGone = (perRound - 1) * c.VolleyPeriodTicks + c.VolleyCueTicks + c.TargetLifeTicks;
            Assert.Less(lastGone, c.RoundHuntTicks, "마지막 무리가 사냥 안에서 사라져야 한다");
        }
    }
}
