using UnityEngine;

namespace MathQuestion
{
    // 實際的設定檔含 Token，不進版控；建立方式見 Docs/QuestionModule.md
    [CreateAssetMenu(fileName = "EMathConfig", menuName = "Math Question/EMath Config")]
    public class EMathConfig : ScriptableObject
    {
        [SerializeField] private string _baseUrl = "https://api.emath.math.ncu.edu.tw";
        [SerializeField] private string _token;

        // 網路不穩時寧可早點改用本機題庫，也不要讓小朋友乾等
        [SerializeField] private int _timeoutSeconds = 5;

        public string BaseUrl => _baseUrl.TrimEnd('/');
        public string Token => _token;
        public int TimeoutSeconds => _timeoutSeconds;
        public bool HasToken => !string.IsNullOrWhiteSpace(_token);
    }
}
