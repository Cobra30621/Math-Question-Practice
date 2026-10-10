using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public enum ResultChoice
    {
        PlayAgain,
        ChangeGrade
    }

    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _satietyText;
        [SerializeField] private Text _growthText;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _changeGradeButton;

        private AwaitableCompletionSource<ResultChoice> _choice;

        private void Awake()
        {
            _playAgainButton.onClick.AddListener(() => Choose(ResultChoice.PlayAgain));
            _changeGradeButton.onClick.AddListener(() => Choose(ResultChoice.ChangeGrade));
        }

        // 玩家按下按鈕時完成
        // grownTo 是這局長大到的階段，沒長大時為 null
        public Awaitable<ResultChoice> Show(int firstTryCorrect, int questionCount, int satietyGained,
            int totalSatiety, MonsterStage? grownTo)
        {
            gameObject.SetActive(true);
            _scoreText.text = $"第一次就答對 {firstTryCorrect} / {questionCount} 題";
            _satietyText.text = $"飽足度 +{satietyGained}（累積 {totalSatiety}）";
            _growthText.gameObject.SetActive(grownTo.HasValue);
            if (grownTo.HasValue)
                _growthText.text = $"小怪獸長大了，變成{MonsterStageNames.Of(grownTo.Value)}！";
            _choice = new AwaitableCompletionSource<ResultChoice>();
            return _choice.Awaitable;
        }

        public void Hide() => gameObject.SetActive(false);

        private void Choose(ResultChoice result)
        {
            var choice = _choice;
            _choice = null;
            Hide();
            choice?.SetResult(result);
        }
    }
}
