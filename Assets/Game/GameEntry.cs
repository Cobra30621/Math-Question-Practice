using System.Collections.Generic;
using MathQuestion;
using UnityEngine;

namespace Game
{
    // 場景進入點：建立答題模組，並串起開始、遊戲、結果三個畫面
    public class GameEntry : MonoBehaviour
    {
        private const int QuestionsPerRound = 5;

        // 沒有設定檔（例如別台電腦沒有 Token）時只用本機題庫
        [SerializeField] private EMathConfig _eMathConfig;
        [SerializeField] private StartScreen _startScreen;
        [SerializeField] private GameScreen _gameScreen;
        [SerializeField] private ResultScreen _resultScreen;

        // 里程碑 4 會改成讀取上次選的年級
        [SerializeField] private Grade _defaultGrade = Grade.G2_1;

        private QuestionProvider _provider;
        private IReadOnlyList<Question> _questions;

        private async void Start()
        {
            var online = _eMathConfig != null ? new EMathQuestionSource(_eMathConfig) : null;
            _provider = new QuestionProvider(online, new LocalQuestionSource());

            _gameScreen.Hide();
            _resultScreen.Hide();
            var grade = await _startScreen.ChooseGrade(_defaultGrade);

            while (true)
            {
                ReleaseQuestions();
                _gameScreen.ShowLoading();
                _questions = await _provider.GetQuestions(new QuestionFilter { Grade = grade }, QuestionsPerRound);
                if (this == null)
                    return;

                if (_questions.Count == 0)
                {
                    _gameScreen.Hide();
                    grade = await _startScreen.ChooseGrade(grade, "找不到題目，請檢查網路");
                    continue;
                }

                var round = new QuestionRound(_questions);
                await _gameScreen.Play(round);

                var choice = await _resultScreen.Show(round.FirstTryCorrectCount, round.Count, round.FedCount);
                if (choice == ResultChoice.ChangeGrade)
                    grade = await _startScreen.ChooseGrade(grade);
            }
        }

        private void OnDestroy() => ReleaseQuestions();

        private void ReleaseQuestions()
        {
            if (_questions == null)
                return;
            foreach (var question in _questions)
                question.Release();
            _questions = null;
        }
    }
}
