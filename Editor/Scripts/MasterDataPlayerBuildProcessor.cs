using System.IO;
using UnityEditor.Build;
using UnityEditor.PackageManager;

namespace LOP.MasterData.Editor
{
    /// <summary>
    /// 이 패키지가 들고 있는 Luban <c>.bytes</c>를 플레이어 빌드의 StreamingAssets에 싣는다.
    /// </summary>
    /// <remarks>
    /// Unity가 빌드로 자동 복사하는 StreamingAssets는 <c>Assets/StreamingAssets</c> 하나뿐이고,
    /// 패키지 안의 StreamingAssets 폴더는 복사되지 않는다. 에디터에서는 <c>Packages/…</c> 가상 경로가
    /// 실제 패키지 폴더로 되돌려져 로딩이 되므로, 이 누락은 <em>플레이어 빌드에서만</em> 드러났다
    /// (2026-07-29 게임서버 이미지에 <c>MasterData</c> 폴더가 아예 없어 부팅 실패).
    /// <para>
    /// 여기 쓰는 <c>AddAdditionalPathToStreamingAssets</c>가 "생성물이라 Assets 밖에 있는 파일을
    /// 빌드에 넣는" 용도로 Unity가 문서화한 API이며, Unity 자신의 Addressables도 같은 방식
    /// (<c>AddressablesPlayerBuildProcessor</c>)을 쓴다.
    /// </para>
    /// </remarks>
    public class MasterDataPlayerBuildProcessor : BuildPlayerProcessor
    {
        private const string PackageRelativeSource = "Runtime.Generated/StreamingAssets/MasterData";

        /// <summary>StreamingAssets 안의 목적지. 런타임은 <c>MasterData/{table}.bytes</c>로 읽는다.</summary>
        public const string DestinationInStreamingAssets = "MasterData";

        /// <summary>빌드에 실을 <c>.bytes</c>가 들어 있는 패키지 폴더의 절대 경로.</summary>
        public static string SourceDirectory
        {
            get
            {
                PackageInfo package = PackageInfo.FindForAssembly(typeof(MasterDataPlayerBuildProcessor).Assembly);
                if (package == null)
                {
                    throw new BuildFailedException(
                        "[LOP.MasterData] 이 어셈블리가 속한 패키지를 찾지 못했다 — StreamingAssets 원본 경로를 정할 수 없다.");
                }
                return Path.GetFullPath(Path.Combine(package.resolvedPath, PackageRelativeSource));
            }
        }

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            string source = SourceDirectory;

            // 없는 경로를 넘겨도 조용히 넘어갈 수 있다. 그렇게 되면 이 버그의 원래 증상(빌드는 성공하고
            // 실행 중에 죽음)이 그대로 재발하므로, 빌드 단계에서 소리내어 실패시킨다.
            if (!Directory.Exists(source))
            {
                throw new BuildFailedException($"[LOP.MasterData] StreamingAssets 원본을 찾지 못했다: {source}");
            }

            buildPlayerContext.AddAdditionalPathToStreamingAssets(source, DestinationInStreamingAssets);
        }
    }
}
