using System;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1.Gacha
{
    public enum Zone1GachaRewardKind
    {
        CoinDouble = 0,
        RandomResourceDouble = 1,
        CoinJackpot = 2,
        CoinPenalty100 = 3,
        RandomResourcePenalty10 = 4,
    }

    [Serializable]
    public struct Zone1GachaRollResult
    {
        public Zone1GachaRewardKind Kind;
        public ResourceType AffectedResource;
        public string SummaryLine;
    }

    public static class Zone1GachaRoller
    {
        static readonly ResourceType[] NonCoinResources =
        {
            ResourceType.WeakSouls,
            ResourceType.PureEnergy,
            ResourceType.MemoryShards,
            ResourceType.UnstableSouls,
        };

        public static Zone1GachaRollResult Roll(
            int seed,
            float chanceCoinX2,
            float chanceResourceX2,
            float chanceCoinX5,
            float chanceCoinMinus100,
            float chanceResourceMinus10)
        {
            var rng = new System.Random(seed);
            var u = (float)rng.NextDouble();

            var pJ = Mathf.Clamp01(chanceCoinX5);
            var pC2 = Mathf.Clamp01(chanceCoinX2);
            var pR2 = Mathf.Clamp01(chanceResourceX2);
            var pM100 = Mathf.Clamp01(chanceCoinMinus100);
            var pR10 = Mathf.Clamp01(chanceResourceMinus10);
            var sum = pJ + pC2 + pR2 + pM100 + pR10;
            if (sum <= 0.0001f)
                sum = 1f;
            var t = u * sum;
            if (t < pJ)
                return Build(Zone1GachaRewardKind.CoinJackpot, ResourceType.DarkCoins, "Jackpot: monedas x5");
            t -= pJ;
            if (t < pC2)
                return Build(Zone1GachaRewardKind.CoinDouble, ResourceType.DarkCoins, "Monedas oscuras x2");
            t -= pC2;
            if (t < pR2)
            {
                var res = NonCoinResources[rng.Next(0, NonCoinResources.Length)];
                return Build(Zone1GachaRewardKind.RandomResourceDouble, res, $"{ResourceName(res)} x2");
            }

            t -= pR2;
            if (t < pM100)
                return Build(Zone1GachaRewardKind.CoinPenalty100, ResourceType.DarkCoins, "-100 monedas oscuras");
            var res2 = NonCoinResources[rng.Next(0, NonCoinResources.Length)];
            return Build(Zone1GachaRewardKind.RandomResourcePenalty10, res2, $"-10 {ResourceName(res2)}");
        }

        public static Zone1GachaRollResult RollDefault(int seed) =>
            Roll(
                seed,
                chanceCoinX2: 0.22f,
                chanceResourceX2: 0.23f,
                chanceCoinX5: 0.05f,
                chanceCoinMinus100: 0.25f,
                chanceResourceMinus10: 0.25f);

        static Zone1GachaRollResult Build(Zone1GachaRewardKind kind, ResourceType res, string line) =>
            new Zone1GachaRollResult { Kind = kind, AffectedResource = res, SummaryLine = line };

        public static string ResourceName(ResourceType t) =>
            t switch
            {
                ResourceType.WeakSouls => "Almas débiles",
                ResourceType.PureEnergy => "Energía pura",
                ResourceType.MemoryShards => "Fragmentos de recuerdo",
                ResourceType.UnstableSouls => "Almas inestables",
                ResourceType.DarkCoins => "Monedas oscuras",
                _ => t.ToString()
            };
    }
}
