using System.Collections.Generic;
using NUnit.Framework;
using PuttSeed.Unity;

namespace PuttSeed.Unity.Tests
{
    /// <summary>
    /// The menu's footer line. Three saves, three goals — and the one that
    /// matters most is the save where nothing is left, because a goal line
    /// that invents a goal is worse than the numbers it replaced.
    /// </summary>
    public class NextGoalTests
    {
        [SetUp]
        public void SetUp() => Loc.Apply("en");

        [TearDown]
        public void TearDown() => Loc.Apply("en");

        [Test]
        public void FreshSave_PointsAtTheFirstJourneySkin()
        {
            var goal = NextGoal.For(new SaveData());

            Assert.That(goal.HasValue, Is.True);
            Assert.That(goal!.Value.Text, Is.EqualTo("5 more levels → Lime ball"));
            Assert.That(goal.Value.Panel, Is.EqualTo(GoalPanel.Journey));
        }

        [Test]
        public void OneLevelShort_SaysLevel_NotLevels()
        {
            var data = new SaveData { journeyStars = new List<int> { 3, 3, 3, 3 } };

            Assert.That(NextGoal.For(data)!.Value.Text, Is.EqualTo("1 more level → Lime ball"));
        }

        [Test]
        public void WhenTheJourneyIsAhead_TheNearestGoalIsTheStreak()
        {
            var data = new SaveData { streak = 5 };
            for (int i = 0; i < 50; i++)
            {
                data.journeyStars.Add(1); // fifty levels, fifty stars
            }

            var goal = NextGoal.For(data);

            Assert.That(goal!.Value.Text, Is.EqualTo("2 more days → Seven Days"));
            Assert.That(goal.Value.Panel, Is.EqualTo(GoalPanel.Stats));
        }

        [Test]
        public void NothingLeftToCount_HasNoGoal()
        {
            var data = new SaveData
            {
                streak = 9,
                practicePlayed = 40,
                achievements = new List<string>
                {
                    "streak7", "dailies10", "three_star_10", "practice25",
                },
            };

            for (int i = 0; i < 60; i++)
            {
                data.journeyStars.Add(3); // every journey gate, and 180 stars
            }

            Assert.That(NextGoal.For(data), Is.Null);
        }

        [Test]
        public void TurkishPutsTheRewardFirst()
        {
            Loc.Apply("tr");
            var text = NextGoal.For(new SaveData())!.Value.Text;

            Assert.That(text, Does.Contain("Limon"), $"expected the Turkish skin name in: {text}");
            Assert.That(text, Does.Contain("seviye"), $"expected a Turkish unit in: {text}");
        }
    }
}
