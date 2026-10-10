using System;
using UnityEngine;
using UnityEngine.Networking;

namespace MathQuestion
{
    internal static class StreamingFile
    {
        // Android 的 StreamingAssets 包在 apk 裡，System.IO 讀不到，各平台統一走 UnityWebRequest
        public static async Awaitable<byte[]> Read(string path)
        {
            string uri = path.Contains("://") ? path : new Uri(path).AbsoluteUri;
            using var request = UnityWebRequest.Get(uri);
            await request.SendWebRequest();
            return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.data : null;
        }
    }
}
