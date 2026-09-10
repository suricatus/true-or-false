using NUnit.Framework;
using UnityEngine;

namespace Suricatus.TrueOrFalse.Presentation.Tests
{
    /// <summary>
    /// As faixas de desempenho sao o que o cliente mais mexe no Inspector.
    /// Estes testes garantem que mexer nelas nao produz resultado silenciosamente errado.
    /// </summary>
    public class ScoreTierTests
    {
        private static ThemeConfig.ResultTheme ThreeTiers()
        {
            return new ThemeConfig.ResultTheme
            {
                tiers = new[]
                {
                    new ThemeConfig.ScoreTier { name = "Baixa", minScore = 0, scoreColor = Color.red },
                    new ThemeConfig.ScoreTier { name = "Media", minScore = 301, scoreColor = Color.yellow },
                    new ThemeConfig.ScoreTier { name = "Alta", minScore = 601, scoreColor = Color.green },
                },
            };
        }

        [TestCase(0, "Baixa")]
        [TestCase(300, "Baixa")]
        [TestCase(301, "Media")]
        [TestCase(600, "Media")]
        [TestCase(601, "Alta")]
        [TestCase(5000, "Alta")]
        public void Resolve_PicksTierByScore(int score, string expected)
        {
            Assert.AreEqual(expected, ThreeTiers().Resolve(score).name);
        }

        [Test]
        public void Resolve_WorksWithTiersOutOfOrder()
        {
            var theme = new ThemeConfig.ResultTheme
            {
                tiers = new[]
                {
                    new ThemeConfig.ScoreTier { name = "Alta", minScore = 601 },
                    new ThemeConfig.ScoreTier { name = "Baixa", minScore = 0 },
                    new ThemeConfig.ScoreTier { name = "Media", minScore = 301 },
                },
            };

            Assert.AreEqual("Media", theme.Resolve(450).name);
        }

        [Test]
        public void Resolve_NegativeScoreFallsIntoLowestTier()
        {
            var theme = new ThemeConfig.ResultTheme
            {
                tiers = new[]
                {
                    new ThemeConfig.ScoreTier { name = "Media", minScore = 301 },
                    new ThemeConfig.ScoreTier { name = "Baixa", minScore = 100 },
                },
            };

            Assert.AreEqual("Baixa", theme.Resolve(-50).name);
        }

        [Test]
        public void Resolve_ReturnsNullWhenNoTiersConfigured()
        {
            var theme = new ThemeConfig.ResultTheme { tiers = new ThemeConfig.ScoreTier[0] };
            Assert.IsNull(theme.Resolve(500));
        }

        [Test]
        public void Resolve_SupportsMoreThanThreeTiers()
        {
            var theme = new ThemeConfig.ResultTheme
            {
                tiers = new[]
                {
                    new ThemeConfig.ScoreTier { name = "A", minScore = 0 },
                    new ThemeConfig.ScoreTier { name = "B", minScore = 200 },
                    new ThemeConfig.ScoreTier { name = "C", minScore = 400 },
                    new ThemeConfig.ScoreTier { name = "D", minScore = 600 },
                    new ThemeConfig.ScoreTier { name = "E", minScore = 800 },
                },
            };

            Assert.AreEqual("D", theme.Resolve(700).name);
        }
    }
}
