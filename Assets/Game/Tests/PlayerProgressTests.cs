using MathQuestion;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class PlayerProgressTests
    {
        [Test]
        public void AddSatiety_ReportsGrowthOnlyWhenCrossingThreshold()
        {
            var progress = new PlayerProgress();

            Assert.IsFalse(progress.AddSatiety(14));
            Assert.IsTrue(progress.AddSatiety(1));
            Assert.IsFalse(progress.AddSatiety(1));

            Assert.AreEqual(MonsterStage.Child, progress.Stage);
            Assert.AreEqual(16, progress.TotalSatiety);
        }

        [Test]
        public void JsonRoundTrip_KeepsAllFields()
        {
            var progress = new PlayerProgress { LastGrade = Grade.G5_2 };
            progress.AddSatiety(42);

            var loaded = JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress));

            Assert.AreEqual(42, loaded.TotalSatiety);
            Assert.AreEqual(MonsterStage.Adult, loaded.Stage);
            Assert.AreEqual(Grade.G5_2, loaded.LastGrade);
        }
    }
}
