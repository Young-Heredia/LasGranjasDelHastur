using System;

namespace LasGranjasDelHastur.Zone1
{
    public static class Zone1UIHoverBus
    {
        public static event Action<FarmCell> HoverChanged;

        public static void RaiseHover(FarmCell cell) => HoverChanged?.Invoke(cell);
    }
}

