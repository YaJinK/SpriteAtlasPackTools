using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SpriteAtlasTools.Runtime
{
    public static class AtlasConfig
    {

        private static Dictionary<string, string> spritePathAtlasNameMap = null;

        public static string GetAtlasABName(string path)
        {
            if (spritePathAtlasNameMap == null)
            {
                LoadSpritePathAtlasNameMap(Application.streamingAssetsPath);
            }

            if (spritePathAtlasNameMap != null && spritePathAtlasNameMap.ContainsKey(path))
                return spritePathAtlasNameMap[path];
            else
                return null;
        }

        public static void LoadSpritePathAtlasNameMap(string path)
        {
            string configJsonFilePath = Path.Combine(path, "AtlasConfig.json");
            // Debug.Log(configJsonFilePath);
            System.Uri uri = new System.Uri(configJsonFilePath);
            // Debug.Log(uri);
            UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            UnityWebRequestAsyncOperation requestAOp = webRequest.SendWebRequest();
            while (requestAOp.isDone == false)
            {
            }
            if (!webRequest.isNetworkError && !webRequest.isHttpError)
                spritePathAtlasNameMap = JsonUtility.FromJson<Serialization<string, string>>(webRequest.downloadHandler.text).ToDictionary();
        }
    }
}
