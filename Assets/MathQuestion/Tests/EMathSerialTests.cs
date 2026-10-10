using NUnit.Framework;

namespace MathQuestion.Tests
{
    public class EMathSerialTests
    {
        [Test]
        public void Build_MatchesDocumentedExample()
        {
            Assert.AreEqual("121032001", EMathSerial.Build(Publisher.Ziyou, Grade.G2_1, 3, 2));
        }

        [TestCase(Grade.G1_1, "1110")]
        [TestCase(Grade.G1_2, "1120")]
        [TestCase(Grade.G6_2, "1620")]
        public void Build_EncodesYearAndSemester(Grade grade, string prefix)
        {
            StringAssert.StartsWith(prefix, EMathSerial.Build(Publisher.Ziyou, grade, 1, 2));
        }

        [Test]
        public void Build_EncodesPublisher()
        {
            StringAssert.StartsWith("4", EMathSerial.Build(Publisher.NanYi, Grade.G1_1, 1, 2));
        }
    }
}
