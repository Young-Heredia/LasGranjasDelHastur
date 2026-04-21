using System;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class ProgressionManager : MonoBehaviour
    {
        public event Action Changed;
        public event Action<int> LevelChanged;

        [Header("Initial Values (Zone1 tests)")]
        [SerializeField] private int initialLevel = 1;
        [SerializeField] private int initialXp = 0;

        [Header("XP Curve")]
        [SerializeField] private int baseXpToLevel = 50;
        [SerializeField] private int xpGrowthPerLevel = 25;

        public int Level { get; private set; }
        public int Xp { get; private set; }

        void Awake()
        {
            Level = Mathf.Max(1, initialLevel);
            Xp = Mathf.Max(0, initialXp);
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
                return;
            Xp += amount;

            var leveled = false;
            while (Xp >= XpToNextLevel())
            {
                Xp -= XpToNextLevel();
                Level += 1;
                leveled = true;
            }

            Changed?.Invoke();
            if (leveled)
                LevelChanged?.Invoke(Level);
        }

        public int XpToNextLevel()
        {
            var t = Mathf.Max(1, baseXpToLevel + (Level - 1) * xpGrowthPerLevel);
            return t;
        }

        public float XpProgress01()
        {
            var denom = Mathf.Max(1, XpToNextLevel());
            return Mathf.Clamp01((float)Xp / denom);
        }

        public bool IsCellTypeUnlocked(Zone1CellType type)
        {
            // Required unlocks:
            // Level 1: SoulPit
            // Level 3: EnergyWell
            // Level 5: placeholder for assistant
            // Level 8: EchoChamber
            // BrokenAltar: available later (keep ready) → lvl 10 by default.
            return type switch
            {
                Zone1CellType.SoulPit => Level >= 1,
                Zone1CellType.EnergyWell => Level >= 3,
                Zone1CellType.EchoChamber => Level >= 8,
                Zone1CellType.BrokenAltar => Level >= 10,
                _ => false
            };
        }
    }
}

