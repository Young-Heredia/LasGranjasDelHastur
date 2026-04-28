using UnityEngine;

namespace LasGranjasDelHastur.Core
{
    public enum GameOverOrigin
    {
        Dungeons,
        CondensedCities,
        Celestial
    }

    public static class GlobalTaxLedger
    {
        const int MaxStrikes = 99;

        public static int GetStrikes()
        {
            var d = SaveManager.Instance?.CachedData;
            return d == null ? 0 : Mathf.Max(0, d.globalTaxStrikes);
        }

        public static void SetStrikes(int value)
        {
            if (SaveManager.Instance?.CachedData == null)
                return;
            SaveManager.Instance.CachedData.globalTaxStrikes = Mathf.Clamp(value, 0, MaxStrikes);
        }

        /// <summary>Incrementa multa global y dispara Game Over al llegar a 3.</summary>
        public static void RegisterStrikeFailure(GameOverOrigin origin)
        {
            if (SaveManager.Instance?.CachedData == null)
                return;

            var d = SaveManager.Instance.CachedData;
            if (d.globalTaxStrikes >= 3)
                return;
            d.globalTaxStrikes = Mathf.Min(MaxStrikes, d.globalTaxStrikes + 1);
            if (d.globalTaxStrikes >= 3)
                GlobalGameOverPresenter.Request(origin);
        }

        public static void ClearStrikes()
        {
            SetStrikes(0);
        }
    }
}
