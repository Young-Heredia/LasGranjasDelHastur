using UnityEngine;
using LasGranjasDelHastur;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class WorldCellClickable : MonoBehaviour
    {
        CellManager _cellManager;
        FarmCell _cell;

        public void Bind(CellManager cellManager, FarmCell cell)
        {
            _cellManager = cellManager;
            _cell = cell;
        }

        void OnMouseEnter()
        {
            if (_cellManager == null || _cell == null)
                return;
            Zone1UIHoverBus.RaiseHover(_cell);
        }

        void OnMouseExit()
        {
            Zone1UIHoverBus.RaiseHover(null);
        }

        void OnMouseDown()
        {
            if (_cellManager == null || _cell == null)
                return;

            if (InputAdapter.LeftMouseDownThisFrame())
                _cellManager.SelectCell(_cell);
        }
    }
}

