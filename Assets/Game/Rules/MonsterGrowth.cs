using UnityEngine;

namespace Game
{
    public enum MonsterStage
    {
        Baby,
        Child,
        Adult
    }

    public static class MonsterGrowth
    {
        // 進入各階段需要的累積飽足度；暫定值，試玩後再調
        private static readonly int[] Thresholds = { 0, 15, 40 };

        public static MonsterStage StageFor(int totalSatiety)
        {
            var stage = MonsterStage.Baby;
            for (int i = 0; i < Thresholds.Length; i++)
            {
                if (totalSatiety >= Thresholds[i])
                    stage = (MonsterStage)i;
            }
            return stage;
        }

        // 目前階段到下一階段的進度（0～1），最後一個階段固定是 1
        public static float ProgressToNextStage(int totalSatiety, MonsterStage stage)
        {
            int index = (int)stage;
            if (index >= Thresholds.Length - 1)
                return 1f;
            return Mathf.Clamp01((float)(totalSatiety - Thresholds[index]) / (Thresholds[index + 1] - Thresholds[index]));
        }
    }
}
