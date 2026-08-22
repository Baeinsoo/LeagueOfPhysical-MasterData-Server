using System.IO;
using Luban;
using NUnit.Framework;

namespace LOP.MasterData.Tests
{
    /// <summary>
    /// <c>#FlappyConfig.xlsx</c>의 열 순서가 나중에 바뀌면(예: gravity와 flap_impulse가 자리를
    /// 바꾸면) Luban은 그걸 알 방법이 없다 — 컬럼 이름이 아니라 "몇 번째 열"로 읽기 때문에
    /// 값이 조용히 뒤바뀐다. <see cref="TableFileManifestTests"/>는 파일 *목록*만 지키고
    /// 값은 보지 않으므로, 배포된 <c>.bytes</c>를 직접 읽어 값 자체를 고정해 둔다.
    /// </summary>
    public class FlappyConfigColumnOrderTests
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
        public void 배포된_바이트가_기대하는_일곱_값과_일치한다()
        {
            var tables = LoadTables();
            var config = tables.TbFlappyConfig.GetOrDefault(1);

            Assert.IsNotNull(config, "TbFlappyConfig id=1 행이 없다");

            const string hint = " — 엑셀 열 순서가 바뀌었을 수 있다";
            Assert.AreEqual(11f, config.ForwardSpeed, "ForwardSpeed" + hint);
            Assert.AreEqual(23f, config.FlapImpulse, "FlapImpulse" + hint);
            Assert.AreEqual(70f, config.Gravity, "Gravity" + hint);
            Assert.AreEqual(30f, config.MaxFallSpeed, "MaxFallSpeed" + hint);
            Assert.AreEqual(0.45f, config.BodyRadius, "BodyRadius" + hint);
            Assert.AreEqual(0.9f, config.BodyHeight, "BodyHeight" + hint);
            Assert.AreEqual(0.35f, config.Restitution, "Restitution" + hint);
        }
    }
}
