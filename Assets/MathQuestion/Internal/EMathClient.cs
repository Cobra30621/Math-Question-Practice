using System;
using UnityEngine;
using UnityEngine.Networking;

namespace MathQuestion
{
    // 連不上伺服器或 Token 被拒，表示這段時間線上題庫整個不能用，不是單題失敗
    internal class EMathUnavailableException : Exception
    {
        public EMathUnavailableException(string message) : base(message)
        {
        }
    }

    internal class EMathClient
    {
        private const int ImageRetryCount = 3;

        private readonly EMathConfig _config;
        private readonly int _timeoutSeconds;

        // 遊戲中要快速放棄所以用設定檔的短逾時；下載工具可以等久一點
        public EMathClient(EMathConfig config, int? timeoutSeconds = null)
        {
            _config = config;
            _timeoutSeconds = timeoutSeconds ?? config?.TimeoutSeconds ?? 0;
        }

        // 空陣列表示這個組合沒有題目
        public async Awaitable<EMathProblem[]> GetProblems(string serial, int count)
        {
            using var request = CreateRequest($"{_config.BaseUrl}/problem/serial/{serial}/{count}");
            await request.SendWebRequest();
            ThrowIfUnavailable(request);
            if (request.result != UnityWebRequest.Result.Success)
                return Array.Empty<EMathProblem>();

            try
            {
                var response = JsonUtility.FromJson<EMathProblemResponse>(request.downloadHandler.text);
                return response?.problem ?? Array.Empty<EMathProblem>();
            }
            catch (ArgumentException)
            {
                return Array.Empty<EMathProblem>();
            }
        }

        // 回傳 null 表示重試後仍失敗
        public async Awaitable<byte[]> GetImage(string link)
        {
            for (int attempt = 0; attempt < ImageRetryCount; attempt++)
            {
                using var request = CreateRequest($"{_config.BaseUrl}/problem/{link}");
                await request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    return request.downloadHandler.data;
            }
            return null;
        }

        private UnityWebRequest CreateRequest(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", _config.Token);
            request.timeout = _timeoutSeconds;
            return request;
        }

        private static void ThrowIfUnavailable(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
                throw new EMathUnavailableException(request.error);
            if (request.responseCode is 401 or 403)
                throw new EMathUnavailableException($"Token 被拒絕（HTTP {request.responseCode}）");
        }
    }

    // 欄位名稱必須與 API 的 JSON 一致，JsonUtility 才讀得到
    [Serializable]
    internal class EMathProblemResponse
    {
        public EMathProblem[] problem;
    }

    [Serializable]
    internal class EMathProblem
    {
        public string problemLink;
        public string ansLink;
        public string answer;
        public string choices;
    }
}
