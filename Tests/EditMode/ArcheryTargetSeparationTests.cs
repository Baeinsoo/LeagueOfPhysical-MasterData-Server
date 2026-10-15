using System.Collections.Generic;
using System.IO;
using Luban;
using NUnit.Framework;
using UnityEngine;

namespace LOP.MasterData.Tests
{
    /// <summary>
    /// 활쏘기 표들(<c>#ArcheryConfig</c>·<c>#ArcheryTarget</c>·<c>#ArcheryRing</c>·<c>#ArcheryRange</c>)은
    /// 서로를 모르므로 Luban이 엇갈림을 못 잡는다 — 에러 없이 판만 이상해진다.
    /// 배포된 <c>.bytes</c>를 직접 읽어 그 관계를 지킨다
    /// (<see cref="FlappyConfigColumnOrderTests"/>와 같은 방식 — 값은 배포물에서 읽는다).
    ///
    /// <para>2026-10-16에 원형 사대(5)·양궁 사거리(6) 맵을 지우면서 웨이브 과녁(공)과 그 검사들도 함께
    /// 뺐다. 지금 남은 활쏘기 맵은 한 발 승부(7) 하나다. 클래스 이름은 옛 웨이브 간격 검사에서 왔다.</para>
    /// </summary>
    public class ArcheryTargetSeparationTests
    {
        private const string StreamingAssetsRelative =
            "Packages/com.baegames.lop.masterdata.server/Runtime.Generated/StreamingAssets/MasterData";

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

        //  사거리(1)·한 발 승부(2) 코스는 #ArcheryRange의 자리 줄로 과녁을 띄운다. 줄이 없으면
        //  방이 뜨는 순간 ArcheryConfigProvider가 예외를 던진다 — 여기서 먼저 막는다.
        [Test]
        public void 자리_코스_맵마다_자리_줄이_있다()
        {
            var tables = LoadTables();
            int checkedMaps = 0;

            foreach (var config in tables.TbArcheryConfig.DataList)
            {
                //  자리 줄을 쓰는 것은 사거리(1)·한 발 승부(2)뿐이다 — 웨이브(0)·투석기(3)는 건너뛴다.
                if (config.CourseKind != 1 && config.CourseKind != 2) { continue; }
                checkedMaps++;

                int rows = 0;
                foreach (var row in tables.TbArcheryRange.DataList)
                {
                    if (row.MapId != config.Id) { continue; }
                    rows++;
                    Assert.GreaterOrEqual(row.StandIndex, 0,
                        $"맵 {config.Id}의 줄 {row.Id}가 음수 자리 번호({row.StandIndex})를 가리킨다");
                }
                Assert.Greater(rows, 0,
                    $"맵 {config.Id}은 course_kind={config.CourseKind}인데 TbArcheryRange에 줄이 없다 — 과녁이 영영 안 뜬다");
            }

            Assert.Greater(checkedMaps, 0,
                "사거리·한 발 승부 코스 맵이 하나도 없다 — 아무것도 재지 못했다. #ArcheryConfig.xlsx의 course_kind를 확인할 것");
        }

        //  자리 코스는 range_target_id가 가리키는 과녁 하나를 띠 점수로 맞힌다. 그 과녁이 판이
        //  아니라 공이면 맞은 자리가 늘 가장자리라 띠 점수가 뜻을 잃는다.
        [Test]
        public void 자리_코스_맵이_가리키는_과녁_종류가_판이다()
        {
            var tables = LoadTables();
            int checkedMaps = 0;

            foreach (var config in tables.TbArcheryConfig.DataList)
            {
                //  자리 줄을 쓰는 것은 사거리(1)·한 발 승부(2)뿐이다 — 웨이브(0)·투석기(3)는 건너뛴다.
                if (config.CourseKind != 1 && config.CourseKind != 2) { continue; }
                checkedMaps++;

                var kind = tables.TbArcheryTarget.GetOrDefault(config.RangeTargetId);
                Assert.IsNotNull(kind,
                    $"맵 {config.Id}의 range_target_id({config.RangeTargetId})가 TbArcheryTarget에 없다");
                Assert.AreEqual(1, kind.Shape,
                    $"맵 {config.Id}의 과녁 '{kind.Code}'는 판(shape=1)이어야 한다 — 공은 맞은 자리가 늘 가장자리라 "
                    + "띠 점수가 뜻을 잃는다");
            }

            Assert.Greater(checkedMaps, 0, "사거리·한 발 승부 코스 맵이 하나도 없다 — 아무것도 재지 못했다");
        }

        //  화살 속도·중력의 원본은 LOP-Shared다 — MasterData 패키지는 Shared를 참조하지 않으므로
        //  (클·서 격리) 여기에 베껴 둘 수밖에 없다. 원본이 바뀌면 이 둘도 같이 고쳐야 한다.
        //    원본: LOP.ArcheryAimSystem.MaxSpeed = 65f, LOP.ArcheryTrajectory.Gravity = 9.81f,
        //          LOP.ArcheryAimSystem.FullDrawSeconds = 0.8f
        private const float ArrowMaxSpeed = 65f;
        private const float ArrowGravity = 9.81f;
        private const float FullDrawSeconds = 0.8f;
        private const float TickSeconds = 0.02f;

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

        //  판정이 "한 틱 동안 과녁이 멈춘 것으로 봐도 된다"는 전제로 돌아간다(ArcheryHitSystem 참고).
        //  솟는 과녁의 위아래 움직임뿐 아니라 사거리 과녁의 좌우 흔들림도 같은 전제를 쓴다 —
        //  삼각파라 속도가 일정하므로(방향이 꺾이는 순간만 빼면) 최고 속도는 2×span/period다.
        [Test]
        public void 좌우로_흔드는_과녁도_한_틱에_판_반지름보다_적게_움직인다()
        {
            var tables = LoadTables();

            //  ⚠️ 이 시험은 원래 "흔드는 자리가 하나도 없으면 실패"로 공허 통과를 막았다.
            //  2026-09-19에 좌우 이동을 **설계 판단으로** 껐다(조준 실력을 타이밍이 아니라
            //  겨누기로 두기로 해서 — lateral_span_m 전부 0). 그래서 그 단언은 이제 *거짓을
            //  주장하는* 것이 된다. 공허 통과 방지는 **표가 실제로 로드되는가**로 옮기고,
            //  한 틱 이동량 검사는 그대로 둔다 — 누가 폭을 다시 넣는 순간 다시 걸린다.
            Assert.Greater(tables.TbArcheryRange.DataList.Count, 0,
                "TbArcheryRange가 비었다 — 표가 안 실렸거나 생성이 깨졌다");

            foreach (var row in tables.TbArcheryRange.DataList)
            {
                if (row.LateralSpanM <= 0f) { continue; }   // 안 흔드는 자리는 잴 것이 없다

                var config = tables.TbArcheryConfig.GetOrDefault(row.MapId);
                Assert.IsNotNull(config, $"맵 {row.MapId}의 TbArcheryConfig 행이 없다");
                var faceKind = tables.TbArcheryTarget.GetOrDefault(config.RangeTargetId);
                Assert.IsNotNull(faceKind, $"맵 {row.MapId}의 range_target_id가 TbArcheryTarget에 없다");

                //  삼각파 최고 속도 = 2 x (span/period) — 반 주기에 폭의 두 배(끝→반대 끝→제자리
                //  중 절반)를 오가므로. ArcheryTargetMotion.TriangleWave와 같은 식이다.
                float maxSpeed = 2f * row.LateralSpanM / row.LateralPeriodS;
                float perTick = maxSpeed * TickSeconds;

                Assert.Less(perTick, faceKind.Radius,
                    $"맵 {row.MapId} 자리 {row.StandIndex}: 폭 {row.LateralSpanM}m/주기 {row.LateralPeriodS}초면 "
                    + $"한 틱에 {perTick:F3}m 움직이는데 판 반지름은 {faceKind.Radius:F3}m다 — 판정이 뚫릴 수 있다");
            }
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
