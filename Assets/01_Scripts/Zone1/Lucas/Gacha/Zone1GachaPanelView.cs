using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone1.Gacha
{
    /// <summary>
    /// Referencias de UI del panel Gacha. Vive en la raíz del prefab <c>Zone1GachaPanel</c> (junto al Canvas).
    /// </summary>
    [DisallowMultipleComponent]
    public class Zone1GachaPanelView : MonoBehaviour
    {
        public Canvas rootCanvas;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtHint;
        public TextMeshProUGUI txtResult;
        public TextMeshProUGUI txtSummary;
        public Image imgMachine;
        public Image imgCapsule;
        public Image imgVfx;
        public Image imgResultIcon;
        public Button btnClose;
        public Button btnPull1;
        public Button btnPull5;
    }
}
