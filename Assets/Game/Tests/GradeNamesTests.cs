using MathQuestion;
using NUnit.Framework;

namespace Game.Tests
{
    public class GradeNamesTests
    {
        [TestCase(Grade.G1_1, "一上")]
        [TestCase(Grade.G1_2, "一下")]
        [TestCase(Grade.G4_1, "四上")]
        [TestCase(Grade.G6_2, "六下")]
        public void Of_ReturnsChineseName(Grade grade, string expected)
        {
            Assert.AreEqual(expected, GradeNames.Of(grade));
        }
    }
}
