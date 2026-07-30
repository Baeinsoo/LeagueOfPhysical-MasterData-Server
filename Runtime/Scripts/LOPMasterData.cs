using Cysharp.Threading.Tasks;
using Luban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LOP.MasterData
{
    /// <summary>
    /// Thin server-side wrapper: owns the Luban-generated <see cref="Tables"/> (server projection)
    /// and async-preloads the binary table files. No domain logic. VContainer Singleton.
    /// </summary>
    public class LOPMasterData
    {
        /// <summary>
        /// 이 패키지가 싣고 오는 테이블 파일 stem 목록. 생성물(<c>Tables.cs</c>의 loader 키 = 실제 <c>.bytes</c>)과
        /// 반드시 일치해야 하며, 새 Luban 테이블 추가 시 여기도 갱신해야 한다.
        /// 어긋나면 <see cref="LoadAsync"/>가 Entrance 단계에서 KeyNotFoundException으로 죽는다 — EditMode 테스트가 지킨다.
        /// <para>server projection: SkinAsset is client-only (group c), so it is absent here.</para>
        /// </summary>
        public static readonly System.Collections.Generic.IReadOnlyList<string> TableFiles = new[]
        {
            "tbcharacter", "tbskin", "tbitem", "tbstatuseffect", "tbability", "tbcombatconfig",
            "tbcharacterloadout",
            "tbgamemode", "tbmap", "tbqueue"
        };

        public Tables Tables { get; private set; }

        public async Task LoadAsync()
        {
            var blobs = new Dictionary<string, byte[]>(TableFiles.Count);
            foreach (var name in TableFiles)
            {
                blobs[name] = await LoadBytes($"MasterData/{name}.bytes");
            }
            Tables = new Tables(file => new ByteBuf(blobs[file]));
        }

        private static async Task<byte[]> LoadBytes(string relativePath)
        {
            string uri;
#if UNITY_EDITOR
            // Editor: package StreamingAssets are not merged into Application.streamingAssetsPath.
            // (In a player build Unity does not copy them either — MasterDataPlayerBuildProcessor adds
            //  this folder to the build's StreamingAssets. A test asserts the two paths match.)
            uri = "file://" + Path.GetFullPath(
                $"Packages/com.baegames.lop.masterdata.server/Runtime.Generated/StreamingAssets/{relativePath}");
#elif UNITY_ANDROID
            uri = Path.Combine(Application.streamingAssetsPath, relativePath);
#else
            uri = "file://" + Path.Combine(Application.streamingAssetsPath, relativePath);
#endif
            using var www = UnityWebRequest.Get(uri);
            await www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[LOPMasterData] Failed to load {uri}: {www.error}");
                return Array.Empty<byte>();
            }
            return www.downloadHandler.data;
        }
    }
}
