using System.IO;
using System.Linq;
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
            Assert.AreEqual(180, c.VolleyPeriodTicks, "VolleyPeriodTicks(3.6초)" + hint);
            Assert.AreEqual(35, c.VolleyCueTicks, "VolleyCueTicks(0.7초)" + hint);
            Assert.AreEqual(215, c.VolleyQuietTicks, "VolleyQuietTicks(4.3초 = 준비 + 수명)" + hint);
            Assert.AreEqual(180, c.TargetLifeTicks, "TargetLifeTicks(3.6초)" + hint);
            Assert.AreEqual(0.35f, c.TrapChance, 1e-5f, "TrapChance" + hint);
            Assert.AreEqual(0.28f, c.JackpotChance, 1e-5f, "JackpotChance" + hint);
            Assert.AreEqual(16f, c.SpawnRadius, "SpawnRadius(가로 ±16 m)" + hint);
            Assert.AreEqual(0, c.ArrowsPerStand, "화살통 없음" + hint);
            Assert.AreEqual(180f, c.ArrowMaxSpeedMps, 1e-5f, "ArrowMaxSpeedMps" + hint);
            Assert.AreEqual(90f, c.ArrowMinSpeedMps, 1e-5f, "ArrowMinSpeedMps(최대의 절반)" + hint);
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

        //  2차 spec §2 표(반지름, 점수, 거리대, 가로 속도대). 가중치는 일반만 표에 있다.
        [TestCase("WaterBalloon", 1.0f, 1, 30f, 36f, 2.0f, 3.0f)]
        [TestCase("Watermelon", 0.75f, 2, 38f, 46f, 2.5f, 3.5f)]
        [TestCase("Plate", 0.85f, 2, 36f, 44f, 5.5f, 7.0f)]
        [TestCase("Rocket", 0.45f, 3, 46f, 55f, 0.5f, 1.5f)]
        [TestCase("GoldenBell", 0.8f, 5, 46f, 54f, 1.6f, 2.2f)]
        [TestCase("Pinata", 0.95f, 5, 44f, 52f, 1.8f, 2.4f)]
        [TestCase("BirdNest", 0.75f, -3, 32f, 48f, 2.5f, 3.5f)]
        [TestCase("CheerFlag", 0.8f, -3, 32f, 48f, 2.5f, 3.5f)]
        [TestCase("KidBalloon", 0.8f, -3, 32f, 48f, 2.0f, 3.0f)]
        public void 투석기_표적_값이_spec_표와_같다(string code, float radius, int points, float d0, float d1, float v0, float v1)
        {
            var k = LoadTables().TbArcheryTarget.DataList.FirstOrDefault(x => x.Code == code);
            Assert.IsNotNull(k, code + "이 없다");
            Assert.AreEqual(radius, k.Radius, 1e-5f, code);
            Assert.AreEqual(points, k.Points, code);
            Assert.AreEqual(d0, k.DistanceMinM, 1e-5f, code);
            Assert.AreEqual(d1, k.DistanceMaxM, 1e-5f, code);
            Assert.AreEqual(v0, k.DriftMinMps, 1e-5f, code);
            Assert.AreEqual(v1, k.DriftMaxMps, 1e-5f, code);
            Assert.AreEqual(0, k.Shape, $"{code}: 투석기 표적은 공(판정 구)이다");
            Assert.AreEqual(k.IsTrap, k.StunTicks == 75, $"{code}: 사격 금지(1.5초)는 금지물만");
            Assert.IsFalse(k.IsTrap && k.IsJackpot, code);
        }

        [Test]
        public void 투석기_표적은_일반_넷_대박_둘_금지물_셋이다()
        {
            var kinds = LoadTables().TbArcheryTarget.DataList.Where(k => k.DistanceMaxM > 0f).ToList();
            CollectionAssert.AreEquivalent(new[] { "WaterBalloon", "Watermelon", "Plate", "Rocket" },
                kinds.Where(k => !k.IsTrap && !k.IsJackpot).Select(k => k.Code));
            CollectionAssert.AreEquivalent(new[] { "GoldenBell", "Pinata" }, kinds.Where(k => k.IsJackpot).Select(k => k.Code));
            CollectionAssert.AreEquivalent(new[] { "BirdNest", "CheerFlag", "KidBalloon" }, kinds.Where(k => k.IsTrap).Select(k => k.Code));
            //  일반 가중치 40·25·25·20, 대박 반반, 금지물 셋 중 하나.
            CollectionAssert.AreEqual(new[] { 40, 25, 25, 20 },
                new[] { "WaterBalloon", "Watermelon", "Plate", "Rocket" }.Select(c => kinds.First(k => k.Code == c).Weight));
            Assert.AreEqual(1, kinds.Where(k => k.IsJackpot).Select(k => k.Weight).Distinct().Count(), "대박은 반반");
            Assert.AreEqual(1, kinds.Where(k => k.IsTrap).Select(k => k.Weight).Distinct().Count(), "금지물은 셋 중 하나");
            Assert.IsTrue(kinds.All(k => k.Weight > 0));
        }

        [Test]
        public void 한_틱에_가장_작아진_판정_반지름보다_적게_간다()
        {
            //  판정은 "한 틱 동안 표적이 멈춰 있다"고 본다. 위아래 빠르기 = 솟음 + 포물선이 막 떠오를 때(4·솟는 높이 ÷ 수명)
            //  + 출렁의 최대 빠르기(2π·f·A). 포물선은 맵 13의 수명 동안 한 번 솟았다 내려오므로 처음과 끝이 가장 빠르다.
            //  도는 접시는 옆으로 누우면 반지름이 0.2배까지 작아진다 — 그 크기로 잰다.
            var t = LoadTables();
            float lifeSeconds = t.TbArcheryConfig.GetOrDefault(13).TargetLifeTicks * TickSeconds;
            foreach (var k in t.TbArcheryTarget.DataList.Where(x => x.DistanceMaxM > 0f))
            {
                float vertical = k.ClimbMaxMps + 2f * (float)System.Math.PI * k.BobHz * k.BobAmpM;
                vertical += 4f * k.ArcMaxM / lifeSeconds;
                float speed = (float)System.Math.Sqrt(k.DriftMaxMps * k.DriftMaxMps + vertical * vertical);
                float smallest = k.SpinHz > 0f ? k.Radius * 0.2f : k.Radius;
                Assert.Less(speed * TickSeconds, smallest, $"{k.Code}: 한 틱 {speed * TickSeconds:F3} m ≥ 반지름 {smallest:F3} m — 판정 뚫림");
            }
        }

        [Test]
        public void 움직임_칸이_spec과_같다()
        {
            var t = LoadTables().TbArcheryTarget.DataList.Where(x => x.DistanceMaxM > 0f).ToDictionary(x => x.Code);
            Assert.AreEqual(0.6f, t["WaterBalloon"].BobAmpM, 1e-5f, "물풍선 출렁 ±0.6 m");
            Assert.That(t["WaterBalloon"].BobHz, Is.InRange(0.45f, 0.55f), "물풍선 초당 약 0.5회");
            Assert.AreEqual(3f, t["Watermelon"].ArcMinM, 1e-5f); Assert.AreEqual(4.5f, t["Watermelon"].ArcMaxM, 1e-5f);
            Assert.AreEqual(2.5f, t["Watermelon"].HeightMinM, 1e-5f); Assert.AreEqual(5f, t["Watermelon"].HeightMaxM, 1e-5f);
            Assert.That(t["Plate"].SpinHz, Is.InRange(1.05f, 1.15f), "접시 초당 약 1.1바퀴");
            Assert.AreEqual(3.5f, t["Rocket"].ClimbMinMps, 1e-5f); Assert.AreEqual(5f, t["Rocket"].ClimbMaxMps, 1e-5f);
            Assert.AreEqual(1f, t["Rocket"].HeightMinM, 1e-5f); Assert.AreEqual(3f, t["Rocket"].HeightMaxM, 1e-5f);
            foreach (var c in new[] { "GoldenBell", "Pinata" }) { Assert.AreEqual(0.3f, t[c].BobAmpM, 1e-5f, c + " 느린 출렁 ±0.3 m"); }
            //  시작 높이는 모두 땅 위, 높이 범위가 거꾸로가 아니다.
            foreach (var k in t.Values)
            {
                Assert.Greater(k.HeightMinM, 0f, k.Code);
                Assert.LessOrEqual(k.HeightMinM, k.HeightMaxM, k.Code);
                Assert.LessOrEqual(k.ArcMinM, k.ArcMaxM, k.Code);
                Assert.LessOrEqual(k.ClimbMinMps, k.ClimbMaxMps, k.Code);
                Assert.GreaterOrEqual(k.LateralHalfM, 0f, k.Code);
            }
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
        public void 무리는_라운드마다_여섯이다()
        {
            var c = LoadTables().TbArcheryConfig.GetOrDefault(13);
            int perRound = (c.RoundHuntTicks - c.VolleyQuietTicks + c.VolleyPeriodTicks - 1) / c.VolleyPeriodTicks;
            Assert.AreEqual(6, perRound);
            int lastGone = (perRound - 1) * c.VolleyPeriodTicks + c.VolleyCueTicks + c.TargetLifeTicks;
            Assert.Less(lastGone, c.RoundHuntTicks, "마지막 무리가 사냥 안에서 사라져야 한다");
        }
    }
}
