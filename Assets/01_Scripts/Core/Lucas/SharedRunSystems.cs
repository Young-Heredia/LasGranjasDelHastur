using LasGranjasDelHastur.Zone1;
using UnityEngine;

namespace LasGranjasDelHastur.Core
{
    /// <summary>
    /// Mantiene un único set de sistemas compartidos entre escenas:
    /// monedas/recursos/nivel/XP/multas (via GlobalTaxLedger + TaxManager debt/timers).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SharedRunSystems : MonoBehaviour
    {
        static SharedRunSystems _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Ensure()
        {
            if (_instance != null)
                return;

            var go = new GameObject("SharedRunSystems");
            _instance = go.AddComponent<SharedRunSystems>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Shared managers (do NOT include CellManager/UIManager because those are per-scene).
            EnsureComponent<ResourceManager>("ResourceManager_Shared");
            EnsureComponent<ProgressionManager>("ProgressionManager_Shared");
            EnsureComponent<AssistantManager>("AssistantManager_Shared");
            EnsureComponent<BuyerManager>("BuyerManager_Shared");
            EnsureComponent<TaxManager>("TaxManager_Shared");

            TryHydrateFromSave();
        }

        void TryHydrateFromSave()
        {
            var sm = SaveManager.Instance;
            if (sm == null || sm.CachedData == null || sm.CachedData.zone1 == null || !sm.CachedData.zone1.valid)
                return;

            var data = sm.CachedData.zone1;
            var rm = FindFirstObjectByType<ResourceManager>();
            var pm = FindFirstObjectByType<ProgressionManager>();
            if (rm != null)
            {
                rm.Set(ResourceType.DarkCoins, data.darkCoins);
                rm.Set(ResourceType.WeakSouls, data.weakSouls);
                rm.Set(ResourceType.PureEnergy, data.pureEnergy);
                rm.Set(ResourceType.MemoryShards, data.memoryShards);
                rm.Set(ResourceType.UnstableSouls, data.unstableSouls);
            }
            pm?.SetProgress(data.level, data.xp);
            // Strikes are already global via GlobalTaxLedger; debt/timers are handled by TaxManager instance at runtime.
        }

        T EnsureComponent<T>(string name) where T : Component
        {
            var child = transform.Find(name);
            GameObject go;
            if (child == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(transform, false);
            }
            else
            {
                go = child.gameObject;
            }

            var c = go.GetComponent<T>();
            if (c == null)
                c = go.AddComponent<T>();
            return c;
        }
    }
}

