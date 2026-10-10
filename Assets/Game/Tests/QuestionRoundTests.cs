using System;
using MathQuestion;
using NUnit.Framework;

namespace Game.Tests
{
    public class QuestionRoundTests
    {
        // 每題的正解依序是 answers 裡的數字
        private static QuestionRound CreateRound(params int[] answers)
        {
            var questions = Array.ConvertAll(answers,
                answer => new Question($"q{answer}", null, null, 4, answer, Grade.G1_1, Publisher.Ziyou));
            return new QuestionRound(questions);
        }

        [Test]
        public void Correct_MovesToNextQuestion()
        {
            var round = CreateRound(1, 2);

            Assert.IsTrue(round.Submit(1));

            Assert.AreEqual(2, round.CurrentNumber);
            Assert.AreEqual("q2", round.Current.Id);
        }

        [Test]
        public void Wrong_StaysOnSameQuestion()
        {
            var round = CreateRound(1, 2);

            Assert.IsFalse(round.Submit(3));

            Assert.AreEqual(1, round.CurrentNumber);
            Assert.AreEqual(0, round.FedCount);
        }

        [Test]
        public void CorrectAfterRetry_FeedsButDoesNotCountAsFirstTry()
        {
            var round = CreateRound(1, 2);

            round.Submit(3);
            round.Submit(1);
            round.Submit(2);

            Assert.IsTrue(round.IsFinished);
            Assert.AreEqual(1, round.FirstTryCorrectCount);
            Assert.AreEqual(2, round.FedCount);
        }

        [Test]
        public void Submit_AfterFinished_Throws()
        {
            var round = CreateRound(1);
            round.Submit(1);

            Assert.Throws<InvalidOperationException>(() => round.Submit(1));
        }
    }
}
