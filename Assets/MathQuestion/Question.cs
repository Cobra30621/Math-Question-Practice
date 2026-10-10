using UnityEngine;

namespace MathQuestion
{
    public class Question
    {
        // 與 eMath 的 answer 相同，從 1 起算
        private readonly int _answer;

        public Question(string id, Sprite questionImage, Sprite optionsImage, int choiceCount, int answer,
            Grade grade, Publisher publisher)
        {
            Id = id;
            QuestionImage = questionImage;
            OptionsImage = optionsImage;
            ChoiceCount = choiceCount;
            _answer = answer;
            Grade = grade;
            Publisher = publisher;
        }

        public string Id { get; }
        public Sprite QuestionImage { get; }
        public Sprite OptionsImage { get; }
        public int ChoiceCount { get; }
        public Grade Grade { get; }
        public Publisher Publisher { get; }

        // choice 從 1 起算，與選項圖上的 ①～④ 一致
        public bool IsCorrect(int choice) => choice == _answer;

        // 圖片是執行期建立的，Unity 不會自動回收，用完要釋放
        public void Release()
        {
            ImageLoader.Destroy(QuestionImage);
            ImageLoader.Destroy(OptionsImage);
        }
    }
}
