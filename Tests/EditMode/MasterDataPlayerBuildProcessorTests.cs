using System.IO;
using System.Linq;
using LOP.MasterData.Editor;
using NUnit.Framework;

namespace LOP.MasterData.Tests
{
    /// <summary>
    /// 플레이어 빌드에 <c>.bytes</c>를 싣는 경로가 살아 있는지 검사.
    /// <para>
    /// 이 검사가 없으면 원본 폴더가 옮겨지거나 이름이 바뀐 것을 빌드가 성공한 뒤
    /// <em>실행 중에</em> 알게 된다(2026-07-29 게임서버 부팅 실패가 그 경로였다).
    /// </para>
    /// </summary>
    public class MasterDataPlayerBuildProcessorTests
    {
        /// <summary>런타임 에디터 분기(<see cref="LOPMasterData"/>)가 읽는 경로와 같은 문자열.</summary>
        private const string RuntimeEditorPath =
            "Packages/com.baegames.lop.masterdata.server/Runtime.Generated/StreamingAssets/MasterData";

        [Test]
        public void SourceDirectoryExists()
        {
            Assert.IsTrue(Directory.Exists(MasterDataPlayerBuildProcessor.SourceDirectory),
                "빌드에 실을 원본 폴더가 없다 → 빌드가 실패하거나 MasterData 없는 빌드가 나온다: "
                + MasterDataPlayerBuildProcessor.SourceDirectory);
        }

        [Test]
        public void SourceDirectoryHoldsEveryTableInLoaderList()
        {
            string dir = MasterDataPlayerBuildProcessor.SourceDirectory;
            string[] missing = LOPMasterData.TableFiles
                .Where(stem => !File.Exists(Path.Combine(dir, stem + ".bytes")))
                .ToArray();

            Assert.IsEmpty(missing,
                "로더가 요구하는 테이블이 빌드 원본에 없다 → 플레이어 빌드가 그 테이블 없이 나온다: "
                + string.Join(", ", missing));
        }

        /// <summary>
        /// 에디터가 읽는 폴더와 빌드가 싣는 폴더가 같아야 한다 — 다르면 에디터에선 되고 빌드에선 다른
        /// 데이터가 실린다. 두 경로는 해석 방식이 달라(가상 <c>Packages/</c> 경로 vs <c>resolvedPath</c>)
        /// 한쪽만 고쳐 놓기 쉬우므로 여기서 묶어 둔다.
        /// </summary>
        [Test]
        public void BuildSourceMatchesRuntimeEditorPath()
        {
            Assert.AreEqual(
                Path.GetFullPath(RuntimeEditorPath),
                MasterDataPlayerBuildProcessor.SourceDirectory,
                "에디터가 읽는 경로와 빌드가 싣는 경로가 어긋났다.");
        }
    }
}
