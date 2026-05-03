using System.Collections;
using System.Text;
using LasGranjasDelHastur.Zone1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LasGranjasDelHastur.Zone1.Gacha
{
    [DisallowMultipleComponent]
    public class Zone1GachaController : MonoBehaviour
    {
        public static Zone1GachaController Instance { get; private set; }

        const string DefaultResourcesPrefabPath = "Prefabs/Zone1/Zone1GachaPanel";

        [Header("Prefab UI (opcional)")]
        [Tooltip("Si está vacío: Resources.Load(\"Prefabs/Zone1/Zone1GachaPanel\"). Si tampoco existe, UI generada en runtime. Genera el prefab con el menú Las Granjas → Zone1 → Generar prefab Gacha.")]
        [SerializeField] GameObject gachaPanelPrefab;

        [Header("Costos")]
        [SerializeField, Min(1)] int singlePullCostDarkCoins = 50;
        [SerializeField, Min(1)] int fivePullCostDarkCoins = 200;
        [SerializeField, Min(1)] int fivePullCount = 5;

        [Header("Pesos de resultado (probabilidades relativas)")]
        [SerializeField, Range(0f, 1f)] float weightCoinX2 = 0.22f;
        [SerializeField, Range(0f, 1f)] float weightResourceX2 = 0.23f;
        [SerializeField, Range(0f, 1f)] float weightCoinX5 = 0.05f;
        [SerializeField, Range(0f, 1f)] float weightCoinMinus100 = 0.25f;
        [SerializeField, Range(0f, 1f)] float weightResourceMinus10 = 0.25f;

        [Header("Tiempos animación")]
        [SerializeField, Min(0.1f)] float spinPhaseSeconds = 1.35f;
        [SerializeField, Min(0.05f)] float capsuleOpenFps = 14f;
        [SerializeField, Min(0.05f)] float machineSpinFps = 18f;

        ResourceManager _resources;
        UIManager _ui;
        GameObject _panelPrefabOverride;
        GameObject _root;
        Zone1GachaPanelView _view;
        bool _built;
        bool _busy;
        Coroutine _running;

        /// <summary>Cuántas tiradas completaron en esta sesión (x1 o bloque x5 cuenta como 1 al terminar la secuencia).</summary>
        public int CompletedPullsThisSession { get; private set; }

        public bool IsPanelOpen => _root != null && _root.activeSelf;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            AudioManager.Instance?.StopZone1GachaLoops();
        }

        public void Setup(ResourceManager resources, UIManager uiManager, GameObject panelPrefabOverride = null)
        {
            _resources = resources;
            _ui = uiManager;
            _panelPrefabOverride = panelPrefabOverride;
        }

        public void OpenFromWorld()
        {
            BuildIfNeeded();
            if (_root == null || _view == null)
                return;
            _root.SetActive(true);
            _busy = false;
            _view.txtResult.text = Safe("");
            _view.txtSummary.text = Safe("");
            _view.txtHint.text = Safe($"Tirada x1: {singlePullCostDarkCoins} monedas  |  x{fivePullCount}: {fivePullCostDarkCoins} monedas");
            SetMachineSprite(Zone1GachaArtPaths.MachineReady);
            SetCapsuleSprite(Zone1GachaArtPaths.CapsuleRare);
            _view.imgVfx.gameObject.SetActive(false);
            _view.imgResultIcon.gameObject.SetActive(false);
            AudioManager.Instance?.PlayZone1GachaPanelOpen();
            AudioManager.Instance?.StartZone1GachaReadyHumLoop();
        }

        public void ClosePanel()
        {
            if (_root != null)
                _root.SetActive(false);
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            _busy = false;
            AudioManager.Instance?.StopZone1GachaLoops();
            AudioManager.Instance?.PlayZone1GachaPanelClose();
        }

        void BuildIfNeeded()
        {
            if (_built)
                return;

            var pref = _panelPrefabOverride != null ? _panelPrefabOverride : gachaPanelPrefab;
            if (pref == null)
                pref = Resources.Load<GameObject>(DefaultResourcesPrefabPath);

            if (pref != null)
            {
                _root = Instantiate(pref);
                _root.name = "Zone1GachaOverlay";
                _view = _root.GetComponent<Zone1GachaPanelView>();
                if (_view == null)
                    _view = _root.GetComponentInChildren<Zone1GachaPanelView>(true);
            }
            else
                _root = Zone1GachaUiRuntimeFactory.CreateOverlayRoot(fivePullCount, out _view);

            if (_view == null)
            {
                Debug.LogError("[Zone1Gacha] No se pudo resolver Zone1GachaPanelView en el prefab o la construcción runtime.");
                if (_root != null)
                    Destroy(_root);
                _root = null;
                return;
            }

            WireButtons();
            SetMachineSprite(Zone1GachaArtPaths.MachineIdle);
            SetCapsuleSprite(Zone1GachaArtPaths.CapsuleCommon);
            _root.SetActive(false);
            _built = true;
        }

        void WireButtons()
        {
            _view.btnClose.onClick.RemoveAllListeners();
            _view.btnClose.onClick.AddListener(ClosePanel);
            _view.btnPull1.onClick.RemoveAllListeners();
            _view.btnPull1.onClick.AddListener(() => TryStartPull(single: true));
            _view.btnPull5.onClick.RemoveAllListeners();
            _view.btnPull5.onClick.AddListener(() => TryStartPull(single: false));
        }

        void TryStartPull(bool single)
        {
            if (_busy || _resources == null || _view == null)
                return;
            var cost = single ? singlePullCostDarkCoins : fivePullCostDarkCoins;
            if (_resources.Get(ResourceType.DarkCoins) < cost)
            {
                _view.txtResult.text = Safe("No tienes monedas oscuras suficientes.");
                _view.txtSummary.text = Safe("");
                AudioManager.Instance?.PlayErrorDeniedSoft();
                return;
            }

            if (!_resources.TrySpend(ResourceType.DarkCoins, cost))
            {
                _view.txtResult.text = Safe("No se pudo cobrar la tirada.");
                return;
            }

            AudioManager.Instance?.PlayZone1GachaButtonPressed();
            _ui?.RefreshFromExternalState();
            _busy = true;
            _view.btnPull1.interactable = false;
            _view.btnPull5.interactable = false;
            if (_view.btnClose != null)
                _view.btnClose.interactable = false;
            _running = StartCoroutine(single ? RunSingleSequence() : RunFiveSequence());
        }

        IEnumerator RunSingleSequence()
        {
            var roll = Zone1GachaRoller.Roll(
                NextSeed(),
                weightCoinX2,
                weightResourceX2,
                weightCoinX5,
                weightCoinMinus100,
                weightResourceMinus10);
            yield return PlayRollPresentation(roll);
            ApplyEconomy(roll);
            _view.txtSummary.text = Safe(roll.SummaryLine);
            _ui?.RefreshFromExternalState();
            FinishPull();
        }

        IEnumerator RunFiveSequence()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < fivePullCount; i++)
            {
                var roll = Zone1GachaRoller.Roll(
                    NextSeed(),
                    weightCoinX2,
                    weightResourceX2,
                    weightCoinX5,
                    weightCoinMinus100,
                    weightResourceMinus10);
                yield return PlayRollPresentation(roll);
                ApplyEconomy(roll);
                _ui?.RefreshFromExternalState();
                sb.AppendLine($"{i + 1}. {roll.SummaryLine}");
            }

            _view.txtSummary.text = Safe(sb.ToString());
            FinishPull();
        }

        /// <summary>
        /// Secuencia de audio (pack <c>zone1_gacha_sfx_pack</c>, rutas en <see cref="Zone1GachaArtPaths"/>):
        /// pull → giro (loop + vfx_spin_glow) → stop → shake → apertura cápsula (open common/cursed/jackpot) →
        /// lead VFX (stinger jackpot / curse_smoke sanciones) → outcome post-VFX (reward_*, penalty_*, reveal_good/bad).
        /// panel_open/close y ready_hum en abrir/cerrar panel; button_pressed al cobrar tirada.
        /// </summary>
        IEnumerator PlayRollPresentation(Zone1GachaRollResult roll)
        {
            AudioManager.Instance?.PlayZone1GachaButtonPull();
            AudioManager.Instance?.StopZone1GachaHumLoop();
            SetMachineSprite(Zone1GachaArtPaths.MachineActive);
            AudioManager.Instance?.PlayZone1GachaCapsuleDrop();
            AudioManager.Instance?.StartZone1GachaSpinLoop();
            AudioManager.Instance?.PlayZone1GachaVfxSpinGlow(0.42f);
            SetCapsuleForKind(roll.Kind, closed: true);
            yield return PlaySheetOnImage(_view.imgMachine, Zone1GachaArtPaths.MachineSpinSheet, 128, 128, spinPhaseSeconds, machineSpinFps);
            AudioManager.Instance?.StopZone1GachaSpinLoop();
            AudioManager.Instance?.PlayZone1GachaMachineStop();
            SetMachineSpriteForResult(roll.Kind);
            AudioManager.Instance?.PlayZone1GachaCapsuleShake();
            yield return PlayCapsuleOpen(roll.Kind);
            // SFX alineado con VFX: stinger/curse al abrir el burst; recompensa o multa + reveal al terminar.
            PlayGachaVfxLeadAudio(roll.Kind);
            yield return PlayVfxBurst(roll.Kind);
            PlayGachaOutcomeAudio(roll.Kind);
            ShowResultIcon(roll.Kind);
            _view.txtResult.text = Safe(roll.SummaryLine);
        }

        void FinishPull()
        {
            _busy = false;
            _view.btnPull1.interactable = true;
            _view.btnPull5.interactable = true;
            if (_view.btnClose != null)
                _view.btnClose.interactable = true;
            _running = null;
            SetMachineSprite(Zone1GachaArtPaths.MachineReady);
            AudioManager.Instance?.StartZone1GachaReadyHumLoop();
            CompletedPullsThisSession++;
        }

        void ApplyEconomy(Zone1GachaRollResult roll)
        {
            if (_resources == null)
                return;
            switch (roll.Kind)
            {
                case Zone1GachaRewardKind.CoinDouble:
                    _resources.Multiply(ResourceType.DarkCoins, 2f);
                    break;
                case Zone1GachaRewardKind.RandomResourceDouble:
                    _resources.Multiply(roll.AffectedResource, 2f);
                    break;
                case Zone1GachaRewardKind.CoinJackpot:
                    _resources.Multiply(ResourceType.DarkCoins, 5f);
                    break;
                case Zone1GachaRewardKind.CoinPenalty100:
                {
                    var n = Mathf.Min(100, _resources.Get(ResourceType.DarkCoins));
                    if (n > 0)
                        _resources.TrySpend(ResourceType.DarkCoins, n);
                    break;
                }
                case Zone1GachaRewardKind.RandomResourcePenalty10:
                {
                    var n = Mathf.Min(10, _resources.Get(roll.AffectedResource));
                    if (n > 0)
                        _resources.TrySpend(roll.AffectedResource, n);
                    break;
                }
            }
        }

        void SetMachineSpriteForResult(Zone1GachaRewardKind k)
        {
            switch (k)
            {
                case Zone1GachaRewardKind.CoinJackpot:
                    SetMachineSprite(Zone1GachaArtPaths.MachineJackpot);
                    break;
                case Zone1GachaRewardKind.CoinPenalty100:
                case Zone1GachaRewardKind.RandomResourcePenalty10:
                    SetMachineSprite(Zone1GachaArtPaths.MachineCursed);
                    break;
                default:
                    SetMachineSprite(Zone1GachaArtPaths.MachineSuccess);
                    break;
            }
        }

        void SetCapsuleForKind(Zone1GachaRewardKind k, bool closed)
        {
            if (!closed)
                return;
            switch (k)
            {
                case Zone1GachaRewardKind.CoinJackpot:
                    SetCapsuleSprite(Zone1GachaArtPaths.CapsuleJackpot);
                    break;
                case Zone1GachaRewardKind.CoinPenalty100:
                case Zone1GachaRewardKind.RandomResourcePenalty10:
                    SetCapsuleSprite(Zone1GachaArtPaths.CapsuleCursed);
                    break;
                default:
                    SetCapsuleSprite(Zone1GachaArtPaths.CapsuleRare);
                    break;
            }
        }

        IEnumerator PlayCapsuleOpen(Zone1GachaRewardKind k)
        {
            if (k == Zone1GachaRewardKind.CoinJackpot)
                AudioManager.Instance?.PlayZone1GachaCapsuleOpenJackpot();
            else if (k is Zone1GachaRewardKind.CoinPenalty100 or Zone1GachaRewardKind.RandomResourcePenalty10)
                AudioManager.Instance?.PlayZone1GachaCapsuleOpenCursed();
            else
                AudioManager.Instance?.PlayZone1GachaCapsuleOpenCommon();

            var path = k switch
            {
                Zone1GachaRewardKind.CoinJackpot => Zone1GachaArtPaths.CapsuleOpenJackpotSheet,
                Zone1GachaRewardKind.CoinPenalty100 or Zone1GachaRewardKind.RandomResourcePenalty10 => Zone1GachaArtPaths.CapsuleOpenCursedSheet,
                _ => Zone1GachaArtPaths.CapsuleOpenCommonSheet,
            };
            var frameCount = k == Zone1GachaRewardKind.CoinJackpot ? 8 : 6;
            yield return PlaySheetOnImage(_view.imgCapsule, path, 64, 64, frameCount / Mathf.Max(1f, capsuleOpenFps), capsuleOpenFps);
        }

        IEnumerator PlayVfxBurst(Zone1GachaRewardKind k)
        {
            _view.imgVfx.gameObject.SetActive(true);
            var path = k switch
            {
                Zone1GachaRewardKind.CoinJackpot => Zone1GachaArtPaths.VfxJackpotBurstSheet,
                Zone1GachaRewardKind.CoinPenalty100 or Zone1GachaRewardKind.RandomResourcePenalty10 => Zone1GachaArtPaths.VfxCurseSmokeSheet,
                _ => Zone1GachaArtPaths.VfxRewardPopSheet,
            };
            var w = k == Zone1GachaRewardKind.CoinJackpot ? 96 : 64;
            var h = k == Zone1GachaRewardKind.CoinJackpot ? 96 : 64;
            var frameCount = k == Zone1GachaRewardKind.CoinJackpot ? 8 : 6;
            yield return PlaySheetOnImage(_view.imgVfx, path, w, h, frameCount / Mathf.Max(1f, capsuleOpenFps), capsuleOpenFps);
            _view.imgVfx.gameObject.SetActive(false);
        }

        /// <summary>
        /// One-shots pensados para coincidir con el inicio del burst VFX (humo maldito / jackpot).
        /// </summary>
        static void PlayGachaVfxLeadAudio(Zone1GachaRewardKind k)
        {
            switch (k)
            {
                case Zone1GachaRewardKind.CoinJackpot:
                    AudioManager.Instance?.PlayZone1GachaJackpotStinger();
                    break;
                case Zone1GachaRewardKind.CoinPenalty100:
                case Zone1GachaRewardKind.RandomResourcePenalty10:
                    AudioManager.Instance?.PlayZone1GachaCurseSmoke();
                    break;
            }
        }

        /// <summary>
        /// Recompensa o sanción concreta + reveal, tras terminar el VFX (coincide con icono y texto).
        /// </summary>
        static void PlayGachaOutcomeAudio(Zone1GachaRewardKind k)
        {
            switch (k)
            {
                case Zone1GachaRewardKind.CoinDouble:
                    AudioManager.Instance?.PlayZone1GachaRewardCoinX2();
                    AudioManager.Instance?.PlayZone1GachaRevealGood();
                    break;
                case Zone1GachaRewardKind.RandomResourceDouble:
                    AudioManager.Instance?.PlayZone1GachaRewardResourceX2();
                    AudioManager.Instance?.PlayZone1GachaRevealGood();
                    break;
                case Zone1GachaRewardKind.CoinJackpot:
                    AudioManager.Instance?.PlayZone1GachaRewardCoinX5();
                    AudioManager.Instance?.PlayZone1GachaRevealGood();
                    break;
                case Zone1GachaRewardKind.CoinPenalty100:
                    AudioManager.Instance?.PlayZone1GachaPenaltyCoin100();
                    AudioManager.Instance?.PlayZone1GachaRevealBad();
                    break;
                case Zone1GachaRewardKind.RandomResourcePenalty10:
                    AudioManager.Instance?.PlayZone1GachaPenaltyResource10();
                    AudioManager.Instance?.PlayZone1GachaRevealBad();
                    break;
            }
        }

        void ShowResultIcon(Zone1GachaRewardKind k)
        {
            string path = k switch
            {
                Zone1GachaRewardKind.CoinDouble => Zone1GachaArtPaths.RewardCoinX2,
                Zone1GachaRewardKind.RandomResourceDouble => Zone1GachaArtPaths.RewardResourceX2,
                Zone1GachaRewardKind.CoinJackpot => Zone1GachaArtPaths.RewardCoinX5,
                Zone1GachaRewardKind.CoinPenalty100 => Zone1GachaArtPaths.PenaltyCoin100,
                Zone1GachaRewardKind.RandomResourcePenalty10 => Zone1GachaArtPaths.PenaltyResource10,
                _ => Zone1GachaArtPaths.PenaltyCurse,
            };
            var sp = Zone1ArtProvider.LoadSprite(path);
            if (sp != null)
            {
                _view.imgResultIcon.sprite = sp;
                _view.imgResultIcon.gameObject.SetActive(true);
            }
            else
                _view.imgResultIcon.gameObject.SetActive(false);
        }

        IEnumerator PlaySheetOnImage(Image img, string sheetPath, int fw, int fh, float maxDuration, float fps)
        {
            var frames = Zone1ArtProvider.LoadSheet(sheetPath, fw, fh);
            if (frames == null || frames.Length == 0)
                yield break;
            var frameTime = 1f / Mathf.Max(1f, fps);
            var elapsed = 0f;
            var n = frames.Length;
            while (elapsed < maxDuration)
            {
                var fi = Mathf.Min(n - 1, Mathf.FloorToInt(elapsed / frameTime) % Mathf.Max(1, n));
                img.sprite = frames[fi];
                elapsed += frameTime;
                yield return new WaitForSecondsRealtime(frameTime);
            }
        }

        static int NextSeed() =>
            UnityEngine.Random.Range(int.MinValue / 2, int.MaxValue / 2) ^ (Time.frameCount * 397);

        void SetMachineSprite(string path)
        {
            var s = Zone1ArtProvider.LoadSprite(path);
            if (s != null && _view != null)
                _view.imgMachine.sprite = s;
        }

        void SetCapsuleSprite(string path)
        {
            var s = Zone1ArtProvider.LoadSprite(path);
            if (s != null && _view != null)
                _view.imgCapsule.sprite = s;
        }

        static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return UIManager.SafeGlyphs(s);
        }
    }
}
