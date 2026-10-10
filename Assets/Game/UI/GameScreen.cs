using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 遊戲畫面：顯示題目、接收拖曳，答題規則交給 QuestionRound
    public class GameScreen : MonoBehaviour
    {
        [SerializeField] private Image _questionImage;
        [SerializeField] private Image _optionsImage;
        [SerializeField] private Text _progressText;
        [SerializeField] private Text _messageText;
        [SerializeField] private MonsterView _monster;
        [SerializeField] private Text _satietyLabel;
        [SerializeField] private RectTransform _satietyFill;
        [SerializeField] private FoodItem[] _foods;
        [SerializeField] private float _foodSpacing = 300f;

        private QuestionRound _round;
        private MonsterStage _stage;
        private AwaitableCompletionSource _finished;
        private bool _isBusy;

        // 每次答對時通知，由進入點累加飽足度並存檔
        public event Action Fed;

        private void Awake()
        {
            foreach (var food in _foods)
            {
                food.DragStarted += OnFoodDragStarted;
                food.Released += OnFoodReleased;
                food.gameObject.SetActive(false);
            }
        }

        public void ShowProgress(PlayerProgress progress)
        {
            _stage = progress.Stage;
            _satietyLabel.text = $"飽足度（{MonsterStageNames.Of(progress.Stage)}）";
            _satietyFill.anchorMax = new Vector2(progress.StageProgress, 1);
        }

        // 上一局的圖片可能已經釋放，取題期間先清掉
        public void ShowLoading()
        {
            gameObject.SetActive(true);
            _monster.SetStage(_stage);
            SetImage(_questionImage, null);
            SetImage(_optionsImage, null);
            _progressText.text = "";
            foreach (var food in _foods)
                food.gameObject.SetActive(false);
            ShowMessage("準備食物中…");
        }

        // 整局答完時完成
        public Awaitable Play(QuestionRound round)
        {
            gameObject.SetActive(true);
            _round = round;
            _finished = new AwaitableCompletionSource();
            ShowQuestion();
            return _finished.Awaitable;
        }

        public void Hide() => gameObject.SetActive(false);

        private void ShowMessage(string message)
        {
            _messageText.text = message;
            _messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private static void SetImage(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private void ShowQuestion()
        {
            var question = _round.Current;
            SetImage(_questionImage, question.QuestionImage);
            SetImage(_optionsImage, question.OptionsImage);
            _progressText.text = $"第 {_round.CurrentNumber} / {_round.Count} 題";
            ShowMessage(null);

            int count = Mathf.Min(question.ChoiceCount, _foods.Length);
            for (int i = 0; i < _foods.Length; i++)
            {
                if (i >= count)
                {
                    _foods[i].gameObject.SetActive(false);
                    continue;
                }
                float x = (i - (count - 1) / 2f) * _foodSpacing;
                _foods[i].Setup(i + 1, new Vector2(x, 0));
            }
            SetFoodsDraggable(true);
        }

        private void OnFoodDragStarted(FoodItem food)
        {
            // 一次只處理一個食物，避免兩隻手指同時拖
            SetFoodsDraggable(false);
            _monster.SetMouthOpen(true);
        }

        private async void OnFoodReleased(FoodItem food, Vector2 screenPoint)
        {
            if (_isBusy)
                return;
            _isBusy = true;
            try
            {
                await Feed(food, screenPoint);
            }
            catch (OperationCanceledException)
            {
                // 畫面被關掉時動畫會取消，不用處理
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Awaitable Feed(FoodItem food, Vector2 screenPoint)
        {
            if (!_monster.Contains(screenPoint, null))
            {
                _monster.SetMouthOpen(false);
                await food.ReturnHome();
                SetFoodsDraggable(true);
                return;
            }

            if (_round.Submit(food.Choice))
            {
                await food.FlyInto(_monster.MouthPosition);
                await _monster.PlayEat();
                Fed?.Invoke();
                if (_monster.Stage != _stage)
                {
                    ShowMessage("長大了！");
                    await _monster.GrowTo(_stage);
                }

                if (_round.IsFinished)
                    Finish();
                else
                    ShowQuestion();
            }
            else
            {
                await _monster.PlaySpit();
                await food.ReturnHome();
                SetFoodsDraggable(true);
            }
        }

        private void Finish()
        {
            var finished = _finished;
            _finished = null;
            Hide();
            finished?.SetResult();
        }

        private void SetFoodsDraggable(bool draggable)
        {
            foreach (var food in _foods)
                food.Draggable = draggable;
        }
    }
}
