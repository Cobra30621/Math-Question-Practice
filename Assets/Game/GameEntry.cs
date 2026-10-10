using System.Collections.Generic;
using MathQuestion;
using UnityEngine;

namespace Game
{
    // 場景進入點：建立答題模組與存檔，並串起開始、遊戲、結果三個畫面
    public class GameEntry : MonoBehaviour
    {
        private const int QuestionsPerRound = 5;

        // 沒有設定檔（例如別台電腦沒有 Token）時只用本機題庫
        [SerializeField] private EMathConfig _eMathConfig;
        [SerializeField] private StartScreen _startScreen;
        [SerializeField] private GameScreen _gameScreen;
        [SerializeField] private ResultScreen _resultScreen;

        private readonly ProgressStore _store = new();
        private PlayerProgress _progress;
        private QuestionProvider _provider;
        private IReadOnlyList<Question> _questions;
        private MonsterStage? _grownThisRound;

        private async void Start()
        {
            var online = _eMathConfig != null ? new EMathQuestionSource(_eMathConfig) : null;
            _provider = new QuestionProvider(online, new LocalQuestionSource());
            _progress = _store.Load();
            _gameScreen.Fed += OnFed;

            _gameScreen.Hide();
            _resultScreen.Hide();
            var grade = await ChooseGrade();

            while (true)
            {
                ReleaseQuestions();
                _gameScreen.ShowProgress(_progress);
                _gameScreen.ShowLoading();
                _questions = await _provider.GetQuestions(new QuestionFilter { Grade = grade }, QuestionsPerRound);
                if (this == null)
                    return;

                if (_questions.Count == 0)
                {
                    _gameScreen.Hide();
                    grade = await ChooseGrade("找不到題目，請檢查網路");
                    continue;
                }

                var round = new QuestionRound(_questions);
                _grownThisRound = null;
                await _gameScreen.Play(round);

                var choice = await _resultScreen.Show(round.FirstTryCorrectCount, round.Count, round.FedCount,
                    _progress.TotalSatiety, _grownThisRound);
                if (choice == ResultChoice.ChangeGrade)
                    grade = await ChooseGrade();
            }
        }

        private async Awaitable<Grade> ChooseGrade(string message = null)
        {
            var grade = await _startScreen.ChooseGrade(_progress.LastGrade, message);
            _progress.LastGrade = grade;
            _store.Save(_progress);
            return grade;
        }

        // 每餵一次就存，玩到一半關掉 App 也不會少算
        private void OnFed()
        {
            if (_progress.AddSatiety(1))
                _grownThisRound = _progress.Stage;
            _store.Save(_progress);
            _gameScreen.ShowProgress(_progress);
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
