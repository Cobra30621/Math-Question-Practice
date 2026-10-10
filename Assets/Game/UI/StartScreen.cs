using MathQuestion;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class StartScreen : MonoBehaviour
    {
        // 依 Grade 的數值順序排列
        [SerializeField] private Button[] _gradeButtons;
        [SerializeField] private Button _startButton;
        [SerializeField] private Text _messageText;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = new(1f, 0.75f, 0.3f);

        private Grade _selected;
        private AwaitableCompletionSource<Grade> _choice;

        private void Awake()
        {
            for (int i = 0; i < _gradeButtons.Length; i++)
            {
                var grade = (Grade)i;
                _gradeButtons[i].GetComponentInChildren<Text>().text = GradeNames.Of(grade);
                _gradeButtons[i].onClick.AddListener(() => Select(grade));
            }
            _startButton.onClick.AddListener(OnStartClicked);
        }

        // 玩家按下開始時完成，回傳選到的年級
        public Awaitable<Grade> ChooseGrade(Grade initial, string message = null)
        {
            gameObject.SetActive(true);
            Select(initial);
            _messageText.text = message ?? "";
            _choice = new AwaitableCompletionSource<Grade>();
            return _choice.Awaitable;
        }

        private void Select(Grade grade)
        {
            _selected = grade;
            for (int i = 0; i < _gradeButtons.Length; i++)
                _gradeButtons[i].image.color = i == (int)grade ? _selectedColor : _normalColor;
        }

        private void OnStartClicked()
        {
            var choice = _choice;
            _choice = null;
            gameObject.SetActive(false);
            choice?.SetResult(_selected);
        }
    }
}
