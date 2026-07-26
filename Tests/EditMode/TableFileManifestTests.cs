using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LOP.MasterData.Tests
{
    /// <summary>
    /// 로더 목록(<see cref="LOPMasterData.TableFiles"/>)이 패키지가 실제로 싣고 오는 <c>.bytes</c>와 일치하는지 검사.
    /// <para>
    /// 새 Luban 테이블을 추가하고 이 목록 갱신을 잊으면 게임이 Entrance 단계에서
    /// <c>KeyNotFoundException</c>으로 죽는다(2026-07-26 <c>tbcharacterloadout</c> 누락으로 실제 발생).
    /// 컴파일도 통과하고 다른 테스트도 전부 통과하기 때문에 이 검사가 유일한 그물이다.
    /// </para>
    /// </summary>
    public class TableFileManifestTests
    {
        private const string StreamingAssetsRelative =
            "Packages/com.baegames.lop.masterdata.server/Runtime.Generated/StreamingAssets/MasterData";

        private static string[] ShippedTableStems()
        {
            string dir = Path.GetFullPath(StreamingAssetsRelative);
            Assert.IsTrue(Directory.Exists(dir), "StreamingAssets 폴더를 찾지 못했다: " + dir);
            return Directory.GetFiles(dir, "*.bytes")
                            .Select(Path.GetFileNameWithoutExtension)
                            .OrderBy(name => name)
                            .ToArray();
        }

        [Test]
        public void LoaderListCoversEveryShippedTable()
        {
            string[] missing = ShippedTableStems()
                .Where(stem => !LOPMasterData.TableFiles.Contains(stem))
                .ToArray();

            Assert.IsEmpty(missing,
                "생성물에 있는데 로더 목록에 없다 → 런타임 로드가 KeyNotFoundException으로 죽는다: "
                + string.Join(", ", missing));
        }

        [Test]
        public void LoaderListHasNoEntryWithoutData()
        {
            string[] shipped = ShippedTableStems();
            string[] stale = LOPMasterData.TableFiles
                .Where(declared => !shipped.Contains(declared))
                .ToArray();

            Assert.IsEmpty(stale,
                "로더 목록에 있는데 실제 .bytes가 없다 → 로드가 빈 배열을 돌려주고 조용히 깨진다: "
                + string.Join(", ", stale));
        }
    }
}
