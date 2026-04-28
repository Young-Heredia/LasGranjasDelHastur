using UnityEngine;

namespace LasGranjasDelHastur.Zone2
{
    public static class Zone2CellLevelRules
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 5;

        public static int ClampLevel(int l) => Mathf.Clamp(l, MinLevel, MaxLevel);

        /// <summary>Suministros o planos por recogida: crece con el nivel; + asistentes; bonus si suma de niveles &gt; cantidad (asistentes mejorados).</summary>
        public static int CollectAmount(int level, int assistants, int assistantLevelSum = -1)
        {
            level = ClampLevel(level);
            var baseAmt = 2 + 3 * level;
            if (assistantLevelSum < 0)
                return baseAmt + assistants;
            var levelBonus = Mathf.Max(0, assistantLevelSum - assistants);
            return baseAmt + assistants + levelBonus / 2;
        }

        /// <summary>Segundos de ciclo base; en juego se divide por el multiplicador de velocidad (dificultad + asistentes).</summary>
        public static float BaseProductionSeconds(int level) =>
            Mathf.Max(0.42f, 6.8f - 1.35f * (ClampLevel(level) - 1));

        public static int NextUpgradeCost(int currentLevel)
        {
            if (currentLevel >= MaxLevel)
                return 0;
            return 40 * currentLevel;
        }

        public static bool CanUpgrade(int level) => level < MaxLevel;

        public static float VisualScaleMultiplier(int level) =>
            1f + (ClampLevel(level) - 1) * 0.06f;

        public static Color LevelTintForMainSprite(int level, bool unlocked)
        {
            if (!unlocked)
                return Color.white;
            var t = (ClampLevel(level) - 1) / 4f;
            return Color.Lerp(Color.white, new Color(0.99f, 0.95f, 0.78f, 1f), t * 0.55f);
        }
    }
}
