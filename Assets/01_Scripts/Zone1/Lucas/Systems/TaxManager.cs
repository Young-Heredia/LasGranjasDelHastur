using System;
using LasGranjasDelHastur.Core;
using UnityEngine;

namespace LasGranjasDelHastur.Zone1
{
    [DisallowMultipleComponent]
    public class TaxManager : MonoBehaviour
    {
        public event Action Changed;
        public event Action AlertOpened;
        public event Action AlertClosed;
        public event Action TaxPaid;
        public event Action TaxFailed;
        public event Action GameOverReached;

        [Header("Zone 1 Taxes")]
        [SerializeField] private string collectorName = "Cthulhu";
        [SerializeField, Range(0f, 1f)] private float baseTaxPercent = 0.15f;

        [Header("Timing")]
        [Tooltip("Zona 1: 20 minutos (1200 s).")]
        [SerializeField] private float taxIntervalSeconds = 1200f;
        [SerializeField] private float payWindowSeconds = 20f;

        [Header("Penalties")]
        [SerializeField, Range(0f, 1f)] private float moneyLossOnFail = 0.75f;
        [SerializeField] private int finePerStrikeStep = 25;
        [SerializeField] private int maxStrikesBeforeGameOver = 3;

        ResourceManager _resources;
        CellManager _cells;

        float _timer;
        float _payWindowRemaining;
        bool _alertActive;
        int _fineDebt;

        public string CollectorName => collectorName;
        public int Strikes => GlobalTaxLedger.GetStrikes();
        public int FineDebt => _fineDebt;
        public bool IsAlertActive => _alertActive;

        public float TimeToNextTaxSeconds => Mathf.Max(0f, _timer);
        public float PayWindowRemainingSeconds => Mathf.Max(0f, _payWindowRemaining);

        void Awake()
        {
            _timer = Mathf.Max(1f, taxIntervalSeconds);
        }

        public void Initialize(ResourceManager resources, CellManager cells)
        {
            _resources = resources;
            _cells = cells;
            _timer = Mathf.Max(1f, taxIntervalSeconds);
            Changed?.Invoke();
        }

        public void Configure(string newCollectorName, float newBaseTaxPercent, float newTaxIntervalSeconds, float newPayWindowSeconds, float newMoneyLossOnFail, int newFinePerStrikeStep, int newMaxStrikesBeforeGameOver)
        {
            collectorName = string.IsNullOrWhiteSpace(newCollectorName) ? "Cthulhu" : newCollectorName;
            baseTaxPercent = Mathf.Clamp01(newBaseTaxPercent);
            taxIntervalSeconds = Mathf.Max(60f, newTaxIntervalSeconds);
            payWindowSeconds = Mathf.Max(5f, newPayWindowSeconds);
            moneyLossOnFail = Mathf.Clamp01(newMoneyLossOnFail);
            finePerStrikeStep = Mathf.Max(1, newFinePerStrikeStep);
            maxStrikesBeforeGameOver = Mathf.Max(1, newMaxStrikesBeforeGameOver);
            _timer = Mathf.Max(1f, taxIntervalSeconds);
            Changed?.Invoke();
        }

        void Update()
        {
            if (_resources == null)
                return;

            if (_alertActive)
            {
                _payWindowRemaining -= Time.deltaTime;
                if (_payWindowRemaining <= 0f)
                {
                    _payWindowRemaining = 0f;
                    FailToPay();
                }

                if (Time.frameCount % 10 == 0)
                    Changed?.Invoke();
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                OpenAlert();
                return;
            }

            if (Time.frameCount % 20 == 0)
                Changed?.Invoke();
        }

        public int CalculateTaxAmount()
        {
            if (_resources == null)
                return 0;

            var money = _resources.Get(ResourceType.DarkCoins);
            var baseAmount = Mathf.FloorToInt(money * baseTaxPercent);
            baseAmount = Mathf.Max(0, baseAmount);
            return baseAmount + Mathf.Max(0, _fineDebt);
        }

        public bool TryPay()
        {
            if (!_alertActive || _resources == null)
                return false;

            var amount = CalculateTaxAmount();
            if (!_resources.TrySpend(ResourceType.DarkCoins, amount))
                return false;

            _fineDebt = 0;
            CloseAlert(resetTimer: true);
            TaxPaid?.Invoke();
            return true;
        }

        void OpenAlert()
        {
            _alertActive = true;
            _payWindowRemaining = Mathf.Max(5f, payWindowSeconds);
            AlertOpened?.Invoke();
            Changed?.Invoke();
        }

        void CloseAlert(bool resetTimer)
        {
            _alertActive = false;
            _payWindowRemaining = 0f;
            if (resetTimer)
                _timer = Mathf.Max(30f, taxIntervalSeconds);
            AlertClosed?.Invoke();
            Changed?.Invoke();
        }

        void FailToPay()
        {
            if (_resources == null)
                return;

            // Lose 75% of money (keep 25%).
            _resources.Multiply(ResourceType.DarkCoins, 1f - moneyLossOnFail);

            GlobalTaxLedger.RegisterStrikeFailure(GameOverOrigin.Dungeons);
            var strikeCount = GlobalTaxLedger.GetStrikes();
            _fineDebt += finePerStrikeStep * strikeCount;

            // Corrupt at least 1 cell if possible.
            _cells?.TryCorruptRandomCell();

            if (AudioManager.Instance != null && AudioManager.Instance.zone1Corruption != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Corruption);

            CloseAlert(resetTimer: true);
            TaxFailed?.Invoke();

            if (strikeCount >= maxStrikesBeforeGameOver)
                GameOverReached?.Invoke();
            Changed?.Invoke();
        }

        public void ApplySaveState(int strikes, int fineDebt, float timeToNextTax, bool alertActive, float payWindowRemaining)
        {
            // Las multas son globales; el valor de guardado legacy se ignora (ver SaveGameData.globalTaxStrikes).
            _fineDebt = Mathf.Max(0, fineDebt);
            _timer = Mathf.Max(0f, timeToNextTax);
            _alertActive = alertActive;
            _payWindowRemaining = Mathf.Max(0f, payWindowRemaining);
            Changed?.Invoke();
        }
    }
}

