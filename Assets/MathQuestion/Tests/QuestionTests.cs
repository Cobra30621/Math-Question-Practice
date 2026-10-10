using NUnit.Framework;

namespace MathQuestion.Tests
{
    public class QuestionTests
    {
        [Test]
        public void IsCorrect_ChoiceAndAnswerAreBothOneBased()
        {
            // 實際題目：answer 為 4、選項為 4 個，正確的是 ④
            var question = new Question("q", null, null, 4, 4, Grade.G1_1, Publisher.Ziyou);

            Assert.IsTrue(question.IsCorrect(4));
            Assert.IsFalse(question.IsCorrect(3));
        }
    }
}
