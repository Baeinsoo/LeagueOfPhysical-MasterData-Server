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
        // server projection: SkinAsset is client-only (group c), so it is absent here.
        private static readonly string[] TableFiles =
        {
            "tbcharacter", "tbskin", "tbaction", "tbitem", "tbstatuseffect"
        };

        public Tables Tables { get; private set; }

        public async Task LoadAsync()
        {
            var blobs = new Dictionary<string, byte[]>(TableFiles.Length);
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
