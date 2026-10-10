using NUnit.Framework;

namespace Game.Tests
{
    public class MonsterGrowthTests
    {
        [TestCase(0, MonsterStage.Baby)]
        [TestCase(14, MonsterStage.Baby)]
        [TestCase(15, MonsterStage.Child)]
        [TestCase(39, MonsterStage.Child)]
        [TestCase(40, MonsterStage.Adult)]
        [TestCase(999, MonsterStage.Adult)]
        public void StageFor_UsesThresholds(int satiety, MonsterStage expected)
        {
            Assert.AreEqual(expected, MonsterGrowth.StageFor(satiety));
        }

        [Test]
        public void ProgressToNextStage_IsFractionOfCurrentStage()
        {
            Assert.AreEqual(0f, MonsterGrowth.ProgressToNextStage(15, MonsterStage.Child), 1e-4);
            Assert.AreEqual(0.4f, MonsterGrowth.ProgressToNextStage(25, MonsterStage.Child), 1e-4);
            Assert.AreEqual(1f, MonsterGrowth.ProgressToNextStage(50, MonsterStage.Adult), 1e-4);
        }

        [Test]
        public void ProgressToNextStage_ClampsWhenSatietyIsBelowStoredStage()
        {
            Assert.AreEqual(0f, MonsterGrowth.ProgressToNextStage(3, MonsterStage.Child), 1e-4);
        }
    }
}
