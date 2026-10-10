using System;
using UnityEngine;

namespace Game
{
    // 進度只有幾個欄位，存成一筆 JSON 放在 PlayerPrefs 就夠了
    public class ProgressStore
    {
        private const string Key = "PlayerProgress";

        public PlayerProgress Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json))
                return new PlayerProgress();

            try
            {
                return JsonUtility.FromJson<PlayerProgress>(json) ?? new PlayerProgress();
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("[Game] 存檔損毀，改用新的進度");
                return new PlayerProgress();
            }
        }

        public void Save(PlayerProgress progress)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }
    }
}
