using System.Collections.Generic;
using System.IO;
using Luban;
using NUnit.Framework;
using UnityEngine;

namespace LOP.MasterData.Tests
{
    /// <summary>
    /// 과녁이 서로 겹치지 않게 막는 장치는 <c>#ArcheryConfig.xlsx</c>의 <c>min_separation</c>
    /// 하나뿐이다 — 중심 사이 거리를 딱 그만큼 띄운다. 그런데 그 값은 과녁 <b>크기</b>를 모른다.
    ///
    /// <para>그래서 <c>#ArcheryTarget.xlsx</c>에 지금보다 큰 과녁을 한 줄 추가하면, 간격은 그대로인데
    /// 과녁만 커져서 <b>겹친 과녁이 뜨기 시작한다</b>. 에러는 나지 않고 화면만 이상해진다.
    /// 두 엑셀은 서로를 모르므로 Luban도 못 잡는다.</para>
    ///
    /// <para>배포된 <c>.bytes</c>를 직접 읽어 그 관계를 지킨다. 겹치지 않으려면 중심이
    /// <b>가장 큰 과녁 둘이 맞닿는 거리</b>(최대 반경 × 2)만큼은 떨어져야 한다.
    /// (<see cref="FlappyConfigColumnOrderTests"/>와 같은 방식 — 값은 배포물에서 읽는다.)</para>
    /// </summary>
    public class ArcheryTargetSeparationTests
    {
        private const string StreamingAssetsRelative =
            "Packages/com.baegames.lop.masterdata.server/Runtime.Generated/StreamingAssets/MasterData";

        //  설정 행의 id가 맵 id로 바뀌었다(활쏘기 맵마다 한 행). 이 파일의 검사들은 지금
        //  존재하는 유일한 활쏘기 맵인 원형 맵(id=5) 행을 기준으로 잰다.
        private const int CircleMapId = 5;

        private static Tables LoadTables()
        {
            string dir = Path.GetFullPath(StreamingAssetsRelative);
            Assert.IsTrue(Directory.Exists(dir), "StreamingAssets 폴더를 찾지 못했다: " + dir);

            return new Tables(name =>
            {
                string path = Path.Combine(dir, name + ".bytes");
                Assert.IsTrue(File.Exists(path), "테이블 파일을 찾지 못했다: " + path);
                return new ByteBuf(File.ReadAllBytes(path));
            });
        }

        //  웨이브가 실제로 뽑을 수 있는 종류만 잰다. 가중치 0은 뽑기에서 절대 안 걸리며
        //  (PickKind가 가중치 합으로 고른다), 사거리 맵이 id로 집어 쓰는 판 과녁이 그것이다.
        //  이 필터가 없으면 사거리용 큰 과녁 한 줄 때문에 원형 맵 규칙이 깨졌다고 잘못 잡는다.
        private static List<ArcheryTargetKind> WaveDrawableKinds(Tables tables)
        {
            var drawable = new List<ArcheryTargetKind>();
            foreach (var row in tables.TbArcheryTarget.DataList)
            {
                if (row.Weight > 0) { drawable.Add(row); }
            }
            Assert.IsNotEmpty(drawable, "웨이브가 뽑을 수 있는 과녁 종류가 하나도 없다 — 웨이브가 영원히 빈다");
            return drawable;
        }

        [Test]
        public void 간격_기준이_가장_큰_과녁_둘을_떼어놓을_만큼은_된다()
        {
            var tables = LoadTables();

            var config = tables.TbArcheryConfig.GetOrDefault(CircleMapId);
            Assert.IsNotNull(config, "TbArcheryConfig 원형 맵(id=5) 행이 없다");

            var rows = WaveDrawableKinds(tables);

            float largestRadius = 0f;
            string largestCode = null;
            foreach (var row in rows)
            {
                if (row.Radius > largestRadius)
                {
                    largestRadius = row.Radius;
                    largestCode = row.Code;
                }
            }

            float touching = largestRadius * 2f;
            Assert.GreaterOrEqual(
                config.MinSeparation, touching,
                $"min_separation({config.MinSeparation})이 가장 큰 과녁 '{largestCode}'(반경 {largestRadius}) "
                + $"둘이 맞닿는 거리({touching})보다 짧다 — 간격을 지켜도 겹친 과녁이 뜬다. "
                + "#ArcheryTarget.xlsx에 큰 과녁을 넣었다면 #ArcheryConfig.xlsx의 min_separation도 같이 올려야 한다");
        }

        //  비율만 올려 두고 함정 종류를 안 넣으면 함정이 영영 안 뜬다 — 에러 없이 게임만 밋밋해진다.
        [Test]
        public void 함정_비율이_0보다_크면_함정_종류가_적어도_하나는_있다()
        {
            var tables = LoadTables();

            var config = tables.TbArcheryConfig.GetOrDefault(CircleMapId);
            Assert.IsNotNull(config, "TbArcheryConfig 원형 맵(id=5) 행이 없다");

            if (config.TrapRatioMax <= 0f)
            {
                Assert.Pass("함정 비율이 0이다 — 함정을 안 쓰기로 한 데이터");
            }

            int trapKinds = 0;
            foreach (var row in WaveDrawableKinds(tables))
            {
                trapKinds += row.IsTrap ? 1 : 0;
            }

            Assert.Greater(trapKinds, 0,
                $"trap_ratio_max({config.TrapRatioMax})가 0보다 큰데 #ArcheryTarget에 is_trap=TRUE인 줄이 없다 "
                + "— 함정이 영영 안 뜬다");
        }

        // 이 슬라이스의 전제: 크기(radius)로 함정을 구분할 수 있으면 안 된다. 함정 종류가 깨끗한
        // 종류와 반경·가중치를 그대로 나눠 가져야, 플레이어가 "큰 과녁 = 함정"을 학습할 수 없다.
        // 반경 하나만 가중치를 바꿔도 이 시험이 잡아야 한다.
        [Test]
        public void 함정과_깨끗한_과녁이_반경별로_같은_가중치를_공유한다()
        {
            var tables = LoadTables();
            var rows = WaveDrawableKinds(tables);

            Dictionary<float, List<int>> CleanOrTrapWeightsByRadius(bool isTrap)
            {
                var byRadius = new Dictionary<float, List<int>>();
                foreach (var row in rows)
                {
                    if (row.IsTrap != isTrap) continue;
                    if (!byRadius.TryGetValue(row.Radius, out var weights))
                    {
                        weights = new List<int>();
                        byRadius[row.Radius] = weights;
                    }
                    weights.Add(row.Weight);
                }
                foreach (var weights in byRadius.Values) weights.Sort();
                return byRadius;
            }

            var cleanByRadius = CleanOrTrapWeightsByRadius(isTrap: false);
            var trapByRadius = CleanOrTrapWeightsByRadius(isTrap: true);

            Assert.IsNotEmpty(cleanByRadius, "깨끗한(is_trap=FALSE) 과녁 종류가 없다");
            Assert.IsNotEmpty(trapByRadius, "함정(is_trap=TRUE) 과녁 종류가 없다");

            foreach (var (radius, cleanWeights) in cleanByRadius)
            {
                Assert.IsTrue(trapByRadius.TryGetValue(radius, out var trapWeights),
                    $"반경 {radius}는 깨끗한 과녁에만 있고 짝이 되는 함정 과녁이 없다 — "
                    + "이 크기를 보면 무조건 안전하다고 학습할 수 있다");

                CollectionAssert.AreEqual(cleanWeights, trapWeights,
                    $"반경 {radius}에서 깨끗한 과녁 가중치({string.Join(",", cleanWeights)})와 "
                    + $"함정 과녁 가중치({string.Join(",", trapWeights)})가 다르다 — "
                    + "같은 크기라도 함정일 확률이 달라지면 크기가 함정을 암시하게 된다");
            }

            foreach (var radius in trapByRadius.Keys)
            {
                Assert.IsTrue(cleanByRadius.ContainsKey(radius),
                    $"반경 {radius}는 함정 과녁에만 있고 짝이 되는 깨끗한 과녁이 없다 — "
                    + "이 크기를 보면 무조건 함정이라고 학습할 수 있다");
            }
        }

        //  웨이브 주기가 묶음 전체를 못 덮으면 마지막 과녁이 공중에서 잘려 사라진다 —
        //  에러는 안 나고 "가끔 과녁이 덜 뜨는" 것으로만 보인다.
        //  ⚠️ 이 식의 원본은 LOP.ArcheryConfig.BurstTicks(LeagueOfPhysical-Shared)다.
        //  MasterData 패키지는 Shared를 참조하지 않으므로(의도된 격리) 여기서 식을 다시
        //  적었다 — 원본 식이 바뀌면 이 검사도 같이 고쳐야 한다.
        [Test]
        public void 웨이브_주기가_묶음_전체와_쉼을_덮는다()
        {
            var tables = LoadTables();
            var config = tables.TbArcheryConfig.GetOrDefault(CircleMapId);
            Assert.IsNotNull(config, "TbArcheryConfig 원형 맵(id=5) 행이 없다");

            //  가장 높이 솟는 과녁이 제일 오래 떠 있다 — 그 기준으로 재야 안전하다.
            //  중력이 화살과 같으므로 v0 = sqrt(2gH), 수명 = 2v0/g다.
            //
            //  ⚠️ 이 식의 **원본은 LOP.ArcheryConfig.BurstTicks**(LeagueOfPhysical-Shared)다.
            //  이 패키지는 Shared를 일부러 참조하지 않으므로(클·서 격리) 같은 식을 여기 다시 적는
            //  수밖에 없다. **저쪽을 고치면 여기도 같이 고쳐야 한다** — 안 그러면 이 검사만 낡은
            //  기준으로 조용히 통과한다. 아래 g와 0.02(틱 간격)도 같은 이유로 베껴 온 값이다.
            float g = 9.81f;   // ArcheryTrajectory.Gravity
            float longestLifetime = 2f * Mathf.Sqrt(2f * g * config.RiseHeightMax) / g;
            //  틱은 정수라 올림한다 — 내림하면 마지막 한 틱이 모자라 과녁이 땅에 닿기 전에 잘린다.
            int lifetimeTicks = Mathf.CeilToInt(longestLifetime / 0.02f);
            int needed = (config.MaxTargets - 1) * config.StaggerTicks + lifetimeTicks + config.RestTicks;

            Assert.GreaterOrEqual(
                config.WavePeriodTicks, needed,
                $"wave_period_ticks({config.WavePeriodTicks})가 묶음 전체({needed}틱: 마지막 과녁이 "
                + $"{(config.MaxTargets - 1) * config.StaggerTicks}틱 뒤에 솟아 {lifetimeTicks}틱을 살고, "
                + $"쉼 {config.RestTicks}틱)보다 짧다 — 마지막 과녁이 공중에서 잘린다");
        }

        //  과녁이 솟는 공간을 벗어나면 사대 위로 넘어오거나 화면 밖으로 나간다.
        [Test]
        public void 솟는_높이가_과녁_공간_안에_들어간다()
        {
            var tables = LoadTables();
            var config = tables.TbArcheryConfig.GetOrDefault(CircleMapId);
            Assert.IsNotNull(config, "TbArcheryConfig 원형 맵(id=5) 행이 없다");

            //  과녁이 솟아 닿는 가장 높은 지점. 무대에서 솟으므로 바닥(y=0) 기준 절대 높이다.
            float apex = config.SpawnMaxY + config.RiseHeightMax;

            //  사수는 12m 밖에 서서 본다 — 이보다 높이 솟으면 위를 쳐다봐야 하고 화면 밖으로 나간다.
            const float HighestVisible = 6f;
            Assert.LessOrEqual(apex, HighestVisible,
                $"가장 높은 자리({config.SpawnMaxY}m)에서 {config.RiseHeightMax}m 솟으면 {apex}m다 — "
                + $"{HighestVisible}m를 넘으면 사수 화면 밖으로 나간다");
        }

        //  이 검사가 이 슬라이스의 생명줄이다 — 높이를 올리면 과녁이 한 틱에 자기 반지름보다
        //  많이 움직여 화살이 뚫고 지나간다. 에러는 안 나고 "가끔 안 맞는다"로만 보인다.
        [Test]
        public void 가장_높이_솟는_과녁도_한_틱에_가장_작은_반지름보다_적게_움직인다()
        {
            var tables = LoadTables();
            var config = tables.TbArcheryConfig.GetOrDefault(CircleMapId);
            Assert.IsNotNull(config, "TbArcheryConfig 원형 맵(id=5) 행이 없다");

            float g = 9.81f;   // ArcheryTrajectory.Gravity
            float fastest = Mathf.Sqrt(2f * g * config.RiseHeightMax);
            float perTick = fastest * 0.02f;

            float smallest = float.MaxValue;
            foreach (var row in tables.TbArcheryTarget.DataList)
            {
                smallest = Mathf.Min(smallest, row.Radius);
            }

            Assert.Less(perTick, smallest,
                $"rise_height_max({config.RiseHeightMax}m)면 과녁이 한 틱에 {perTick:F3}m 움직이는데 "
                + $"가장 작은 과녁 반지름이 {smallest:F3}m다 — 판정이 뚫린다. 높이를 낮추거나 "
                + "가장 작은 과녁을 키워야 한다");
        }

        //  띠가 중심부터 가장자리까지 빈틈없이 덮어야 한다. 마지막 띠가 1에 못 미치면
        //  가장자리에 맞은 화살이 어느 띠에도 안 걸려 점수가 0이 된다 — 에러는 안 난다.
        [Test]
        public void 모든_과녁의_띠가_가장자리까지_덮는다()
        {
            var tables = LoadTables();

            foreach (var target in tables.TbArcheryTarget.DataList)
            {
                var edges = new List<float>();
                foreach (var ring in tables.TbArcheryRing.DataList)
                {
                    if (ring.TargetId == target.Id) { edges.Add(ring.OuterRatio); }
                }

                Assert.IsNotEmpty(edges, $"과녁 {target.Code}(id={target.Id})에 띠가 하나도 없다");
                edges.Sort();
                Assert.AreEqual(1f, edges[edges.Count - 1], 1e-4f,
                    $"과녁 {target.Code}의 마지막 띠가 가장자리(1.0)까지 안 간다");
                Assert.Greater(edges[0], 0f, $"과녁 {target.Code}의 첫 띠 경계가 0 이하다");
            }
        }

        //  공은 겉면에 맞으므로 "중심에서 얼마나 벗어났나"가 늘 1에 가깝다 — 띠를 여러 개 줘도
        //  바깥 띠만 걸린다. 그런 데이터는 적은 사람의 뜻과 다르게 동작하므로 막는다.
        [Test]
        public void 공_과녁은_띠가_하나뿐이다()
        {
            var tables = LoadTables();

            foreach (var target in tables.TbArcheryTarget.DataList)
            {
                if (target.Shape != 0) { continue; }   // 0 = Sphere

                int count = 0;
                foreach (var ring in tables.TbArcheryRing.DataList)
                {
                    if (ring.TargetId == target.Id) { count++; }
                }

                Assert.AreEqual(1, count,
                    $"공 과녁 {target.Code}에 띠가 {count}개다 — 공은 맞은 자리를 가릴 수 없다");
            }
        }

        //  띠가 하나인 과녁은 그 점수가 대표 점수와 같아야 한다. 다르면 "띠 데이터가 없을 때"와
        //  "있을 때"의 점수가 갈리는데, 코드는 둘 다 정상으로 받아들여 조용히 다르게 동작한다.
        [Test]
        public void 띠가_하나인_과녁은_그_점수가_대표_점수와_같다()
        {
            var tables = LoadTables();

            foreach (var target in tables.TbArcheryTarget.DataList)
            {
                var points = new List<int>();
                foreach (var ring in tables.TbArcheryRing.DataList)
                {
                    if (ring.TargetId == target.Id) { points.Add(ring.Points); }
                }
                if (points.Count != 1) { continue; }

                Assert.AreEqual(target.Points, points[0],
                    $"과녁 {target.Code}의 띠 점수({points[0]})가 대표 점수({target.Points})와 다르다");
            }
        }

        //  활쏘기 맵을 새로 추가하면서 설정 행을 안 넣으면, 방에 들어가야 예외를 본다.
        //  여기서 먼저 막는다.
        [Test]
        public void 활쏘기_맵마다_설정_행이_있다()
        {
            var tables = LoadTables();

            int checked_ = 0;
            foreach (var map in tables.TbMap.DataList)
            {
                var mode = tables.TbGameMode.GetOrDefault(map.GameModeId);
                if (mode == null || mode.Code != "Archery") { continue; }

                checked_++;
                Assert.IsNotNull(tables.TbArcheryConfig.GetOrDefault(map.Id),
                    $"활쏘기 맵 {map.Code}(id={map.Id})에 TbArcheryConfig 행이 없다");
            }

            //  활쏘기 맵이 하나도 없으면 위 반복문이 안 돌아 아무것도 안 잰 채 통과한다.
            //  맵을 옮기다 연결이 끊겨도 이 검사가 조용히 초록이 되는 것을 막는다.
            Assert.Greater(checked_, 0, "활쏘기 맵이 하나도 없다 — TbMap의 game_mode_id 연결을 확인할 것");
        }

        //  화살 속도·중력의 원본은 LOP-Shared다 — MasterData 패키지는 Shared를 참조하지 않으므로
        //  (클·서 격리) 여기에 베껴 둘 수밖에 없다. 원본이 바뀌면 이 둘도 같이 고쳐야 한다.
        //    원본: LOP.ArcheryAimSystem.MaxSpeed = 65f, LOP.ArcheryTrajectory.Gravity = 9.81f,
        //          LOP.ArcheryAimSystem.FullDrawSeconds = 0.8f
        private const float ArrowMaxSpeed = 65f;
        private const float ArrowGravity = 9.81f;
        private const float FullDrawSeconds = 0.8f;
        private const float TickSeconds = 0.02f;

        private const int RangeMapId = 6;

        [Test]
        public void 사거리_맵의_거리가_빈틈없이_0부터_이어진다()
        {
            var tables = LoadTables();
            int checkedMaps = 0;

            foreach (var config in tables.TbArcheryConfig.DataList)
            {
                if (config.CourseKind != 1) { continue; }
                checkedMaps++;

                var indices = new List<int>();
                foreach (var row in tables.TbArcheryRange.DataList)
                {
                    if (row.MapId == config.Id) { indices.Add(row.StandIndex); }
                }
                indices.Sort();

                Assert.IsNotEmpty(indices,
                    $"맵 {config.Id}은 사거리 코스인데 TbArcheryRange에 줄이 하나도 없다 — 과녁이 영영 안 뜬다");
                for (int i = 0; i < indices.Count; i++)
                {
                    Assert.AreEqual(i, indices[i],
                        $"맵 {config.Id}의 stand_index가 0부터 빈틈없이 이어지지 않는다: "
                        + string.Join(",", indices) + " — 씬의 과녁 자리 번호와 짝이 안 맞는다");
                }
            }

            Assert.Greater(checkedMaps, 0,
                "사거리 코스 맵이 하나도 없다 — 이 시험은 아무것도 재지 못했다. "
                + "#ArcheryConfig.xlsx의 course_kind를 확인할 것");
        }

        [Test]
        public void 사거리_과녁이_꽉_당겨도_닿는_거리에_있다()
        {
            var tables = LoadTables();
            //  45도로 꽉 당겨 쏜 최대 사거리. 여기가 물리적인 벽이다.
            float maxRange = ArrowMaxSpeed * ArrowMaxSpeed / ArrowGravity;   // 약 430.7m
            //  벽에 딱 붙이면 각도가 1도만 어긋나도 못 닿는다 — 8할까지만 쓴다.
            float usable = maxRange * 0.8f;

            int checkedRows = 0;
            foreach (var row in tables.TbArcheryRange.DataList)
            {
                checkedRows++;
                Assert.LessOrEqual(row.DistanceM, usable,
                    $"거리 {row.DistanceM}m(맵 {row.MapId}, 자리 {row.StandIndex})는 "
                    + $"쓸 수 있는 사거리 {usable:0.#}m를 넘는다 — 그 과녁은 영영 못 맞히는데 에러도 안 난다");
            }
            Assert.Greater(checkedRows, 0, "TbArcheryRange가 비어 있다 — 아무것도 재지 못했다");
        }

        [Test]
        public void 노출_시간이_당기고_날아갈_시간보다_길다()
        {
            var tables = LoadTables();
            int checkedRows = 0;

            foreach (var row in tables.TbArcheryRange.DataList)
            {
                checkedRows++;
                //  45도로 꽉 당겨 쏘면 수평 속도는 65 × cos45 다. 그 거리까지 가는 데 걸리는 시간.
                float horizontalSpeed = ArrowMaxSpeed * Mathf.Cos(45f * Mathf.Deg2Rad);
                float flight = row.DistanceM / horizontalSpeed;
                float needed = FullDrawSeconds + flight;
                float exposure = row.ExposureTicks * TickSeconds;

                Assert.Greater(exposure, needed,
                    $"맵 {row.MapId} 자리 {row.StandIndex}({row.DistanceM}m)의 노출 {exposure:0.##}초는 "
                    + $"꽉 당기고({FullDrawSeconds}초) 날아가는 데({flight:0.##}초) 걸리는 {needed:0.##}초보다 짧다 "
                    + "— 물리적으로 못 맞히는 과녁이 된다");
            }
            Assert.Greater(checkedRows, 0, "TbArcheryRange가 비어 있다 — 아무것도 재지 못했다");
        }

        [Test]
        public void 사거리_맵이_가리키는_과녁_종류가_판이고_웨이브에는_안_뜬다()
        {
            var tables = LoadTables();
            int checkedMaps = 0;

            foreach (var config in tables.TbArcheryConfig.DataList)
            {
                if (config.CourseKind != 1) { continue; }
                checkedMaps++;

                var kind = tables.TbArcheryTarget.GetOrDefault(config.RangeTargetId);
                Assert.IsNotNull(kind,
                    $"맵 {config.Id}의 range_target_id({config.RangeTargetId})가 TbArcheryTarget에 없다");
                Assert.AreEqual(1, kind.Shape,
                    $"사거리 과녁 '{kind.Code}'는 판(shape=1)이어야 한다 — 공은 맞은 자리가 늘 가장자리라 "
                    + "띠 점수가 뜻을 잃는다");
                Assert.AreEqual(0, kind.Weight,
                    $"사거리 과녁 '{kind.Code}'의 가중치가 0이 아니다 — 원형 맵 웨이브가 이 판을 뽑아 "
                    + "허공에 세운다");
            }

            Assert.Greater(checkedMaps, 0, "사거리 코스 맵이 하나도 없다 — 아무것도 재지 못했다");
        }

        [Test]
        public void 판_과녁의_띠가_0부터_1까지_빈틈없이_덮는다()
        {
            var tables = LoadTables();
            int checkedKinds = 0;

            foreach (var kind in tables.TbArcheryTarget.DataList)
            {
                var bands = new List<ArcheryRing>();
                foreach (var ring in tables.TbArcheryRing.DataList)
                {
                    if (ring.TargetId == kind.Id) { bands.Add(ring); }
                }
                if (bands.Count == 0) { continue; }

                checkedKinds++;
                bands.Sort((a, b) => a.OuterRatio.CompareTo(b.OuterRatio));
                Assert.AreEqual(1f, bands[bands.Count - 1].OuterRatio, 1e-4f,
                    $"과녁 '{kind.Code}'의 바깥 띠가 1.0이 아니다 — 가장자리를 맞히면 점수가 엉뚱해진다");
                for (int i = 0; i < bands.Count; i++)
                {
                    Assert.Greater(bands[i].OuterRatio, 0f,
                        $"과녁 '{kind.Code}'에 바깥 경계가 0 이하인 띠가 있다 — 그 띠는 영영 안 걸린다");
                }
            }

            Assert.Greater(checkedKinds, 0, "띠가 달린 과녁이 하나도 없다 — 아무것도 재지 못했다");
        }
    }
}
