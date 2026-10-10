using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace MathQuestion.Tests
{
    public class QuestionProviderTests
    {
        private static readonly QuestionFilter Filter = new() { Grade = Grade.G2_1 };

        [Test]
        public async Task OnlineEnough_DoesNotUseLocal()
        {
            var local = new FakeSource("L1");
            var provider = new QuestionProvider(new FakeSource("O1", "O2"), local);

            var questions = await provider.GetQuestions(Filter, 2);

            CollectionAssert.AreEqual(new[] { "O1", "O2" }, questions.Select(q => q.Id));
            Assert.AreEqual(0, local.CallCount);
        }

        [Test]
        public async Task OnlineShort_FillsFromLocalWithoutDuplicates()
        {
            var provider = new QuestionProvider(new FakeSource("A"), new FakeSource("A", "B", "C"));

            var questions = await provider.GetQuestions(Filter, 3);

            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, questions.Select(q => q.Id));
        }

        [Test]
        public async Task OnlineThrows_FallsBackToLocal()
        {
            var provider = new QuestionProvider(new FakeSource { Throws = true }, new FakeSource("L1", "L2"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new Regex("取題失敗"));

            var questions = await provider.GetQuestions(Filter, 2);

            CollectionAssert.AreEqual(new[] { "L1", "L2" }, questions.Select(q => q.Id));
        }

        [Test]
        public async Task NoOnline_UsesLocalOnly()
        {
            var provider = new QuestionProvider(null, new FakeSource("L1"));

            var questions = await provider.GetQuestions(Filter, 5);

            Assert.AreEqual(1, questions.Count);
        }

        private class FakeSource : IQuestionSource
        {
            private readonly string[] _ids;

            public FakeSource(params string[] ids)
            {
                _ids = ids;
            }

            public bool Throws { get; set; }
            public int CallCount { get; private set; }

            public Awaitable<IReadOnlyList<Question>> GetQuestions(QuestionFilter filter, int count)
            {
                CallCount++;
                if (Throws)
                    throw new InvalidOperationException("fake failure");

                var source = new AwaitableCompletionSource<IReadOnlyList<Question>>();
                source.SetResult(_ids.Take(count)
                    .Select(id => new Question(id, null, null, 4, 0, filter.Grade, filter.Publisher))
                    .ToList());
                return source.Awaitable;
            }
        }
    }
}
