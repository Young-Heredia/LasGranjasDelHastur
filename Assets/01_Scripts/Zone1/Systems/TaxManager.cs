using System;
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
<<<<<<< HEAD:Assets/01_Scripts/Zone1/Systems/TaxManager.cs
=======
        public event Action GameOverReached;
>>>>>>> origin/devlucas:Assets/Scripts/Zone1/Systems/TaxManager.cs

        [Header("Zone 1 Taxes")]
        [SerializeField] private string collectorName = "Cthulhu";
        [SerializeField, Range(0f, 1f)] private float baseTaxPercent = 0.15f;

        [Header("Timing")]
        [Tooltip("For tests in editor; design target is 10 minutes.")]
        [SerializeField] private float taxIntervalSeconds = 90f;
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
        int _strikes;
        int _fineDebt;

        public string CollectorName => collectorName;
        public int Strikes => _strikes;
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
            taxIntervalSeconds = Mathf.Max(5f, newTaxIntervalSeconds);
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

            _strikes += 1;
            _fineDebt += finePerStrikeStep * _strikes;

            // Corrupt at least 1 cell if possible.
            _cells?.TryCorruptRandomCell();

            if (AudioManager.Instance != null && AudioManager.Instance.zone1Corruption != null)
                AudioManager.Instance.PlaySFX(AudioManager.Instance.zone1Corruption);

            CloseAlert(resetTimer: true);
            TaxFailed?.Invoke();

            // Game Over prepared (not hard-implemented yet): strikes >= 3.
            if (_strikes >= maxStrikesBeforeGameOver)
            {
<<<<<<< HEAD:Assets/01_Scripts/Zone1/Systems/TaxManager.cs
                // Prepared hook: UI can show "Game Over" later.
=======
                GameOverReached?.Invoke();
>>>>>>> origin/devlucas:Assets/Scripts/Zone1/Systems/TaxManager.cs
                Changed?.Invoke();
            }
        }

        public void ApplySaveState(int strikes, int fineDebt, float timeToNextTax, bool alertActive, float payWindowRemaining)
        {
            _strikes = Mathf.Max(0, strikes);
            _fineDebt = Mathf.Max(0, fineDebt);
            _timer = Mathf.Max(0f, timeToNextTax);
            _alertActive = alertActive;
            _payWindowRemaining = Mathf.Max(0f, payWindowRemaining);
            Changed?.Invoke();
        }
    }
}

