using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace MathQuestion.Tests
{
    public class LocalQuestionSourceTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            // 路徑刻意含空白，確認轉成 URI 時有正確跳脫
            _root = Path.Combine(Path.GetTempPath(), "MathQuestion Tests " + Path.GetRandomFileName());
            string images = Path.Combine(_root, LocalQuestionIndex.ImagesFolderName);
            Directory.CreateDirectory(images);

            var texture = new Texture2D(2, 2);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            File.WriteAllBytes(Path.Combine(images, "q.png"), png);
            File.WriteAllBytes(Path.Combine(images, "o.png"), png);

            var index = new LocalQuestionIndex
            {
                questions = new[]
                {
                    Entry("g2", Grade.G2_1, "q.png"),
                    Entry("g3", Grade.G3_1, "q.png"),
                    Entry("missing", Grade.G2_1, "missing.png")
                }
            };
            File.WriteAllText(Path.Combine(_root, LocalQuestionIndex.IndexFileName), JsonUtility.ToJson(index));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_root, true);
        }

        [Test]
        public async Task GetQuestions_ReturnsMatchingGradeAndSkipsMissingImages()
        {
            var source = new LocalQuestionSource(_root);

            var questions = await source.GetQuestions(new QuestionFilter { Grade = Grade.G2_1 }, 5);

            Assert.AreEqual(1, questions.Count);
            Assert.AreEqual("g2", questions[0].Id);
            Assert.IsNotNull(questions[0].QuestionImage);
            Assert.IsTrue(questions[0].IsCorrect(1));
            questions[0].Release();
        }

        [Test]
        public async Task GetQuestions_MissingIndex_ReturnsEmpty()
        {
            var source = new LocalQuestionSource(Path.Combine(_root, "nope"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new Regex("找不到本機題庫"));

            var questions = await source.GetQuestions(new QuestionFilter(), 5);

            Assert.AreEqual(0, questions.Count);
        }

        private static LocalQuestionEntry Entry(string id, Grade grade, string questionFile) => new()
        {
            id = id,
            publisher = Publisher.Ziyou.ToString(),
            grade = grade.ToString(),
            question = questionFile,
            options = "o.png",
            answer = 1,
            choices = 4
        };
    }
}
