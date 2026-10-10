using System;
using MathQuestion;
using UnityEngine;

namespace Game
{
    // 需要存檔的玩家進度；欄位會被 JsonUtility 序列化
    [Serializable]
    public class PlayerProgress
    {
        [SerializeField] private int _totalSatiety;
        [SerializeField] private MonsterStage _stage;
        [SerializeField] private Grade _lastGrade = Grade.G2_1;

        public int TotalSatiety => _totalSatiety;
        public MonsterStage Stage => _stage;
        public float StageProgress => MonsterGrowth.ProgressToNextStage(_totalSatiety, _stage);

        public Grade LastGrade
        {
            get => _lastGrade;
            set => _lastGrade = value;
        }

        // 回傳是否長大了一個階段。
        // 階段另外存而且只升不降：試玩後調高門檻，已經長大的怪獸也不會變回去
        public bool AddSatiety(int amount)
        {
            _totalSatiety += amount;
            var stage = MonsterGrowth.StageFor(_totalSatiety);
            if (stage <= _stage)
                return false;
            _stage = stage;
            return true;
        }
    }
}
