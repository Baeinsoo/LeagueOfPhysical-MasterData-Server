using System.IO;
using Luban;
using NUnit.Framework;

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

        [Test]
        public void 간격_기준이_가장_큰_과녁_둘을_떼어놓을_만큼은_된다()
        {
            var tables = LoadTables();

            var config = tables.TbArcheryConfig.GetOrDefault(1);
            Assert.IsNotNull(config, "TbArcheryConfig id=1 행이 없다");

            var rows = tables.TbArcheryTarget.DataList;
            Assert.IsNotEmpty(rows, "TbArcheryTarget이 비어 있다 — 과녁 종류가 없으면 웨이브가 영원히 빈다");

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

            var config = tables.TbArcheryConfig.GetOrDefault(1);
            Assert.IsNotNull(config, "TbArcheryConfig id=1 행이 없다");

            if (config.TrapRatioMax <= 0f)
            {
                Assert.Pass("함정 비율이 0이다 — 함정을 안 쓰기로 한 데이터");
            }

            int trapKinds = 0;
            foreach (var row in tables.TbArcheryTarget.DataList)
            {
                trapKinds += row.IsTrap ? 1 : 0;
            }

            Assert.Greater(trapKinds, 0,
                $"trap_ratio_max({config.TrapRatioMax})가 0보다 큰데 #ArcheryTarget에 is_trap=TRUE인 줄이 없다 "
                + "— 함정이 영영 안 뜬다");
        }
    }
}
