using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LasGranjasDelHastur
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(EventSystem))]
    public class EventSystemInputModuleBridge : MonoBehaviour
    {
        static bool _sceneHookInstalled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnforceAllEventSystemsAfterSceneLoad()
        {
            EnforceAllEventSystems();

            if (_sceneHookInstalled)
                return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            _sceneHookInstalled = true;
        }

        static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            EnforceAllEventSystems();
        }

        static void EnforceAllEventSystems()
        {
            var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            foreach (var es in all)
            {
                if (es == null)
                    continue;
                var bridge = es.GetComponent<EventSystemInputModuleBridge>();
                if (bridge == null)
                    bridge = es.gameObject.AddComponent<EventSystemInputModuleBridge>();
                bridge.EnsureCorrectInputModule();
            }
        }

        void Awake()
        {
            EnsureCorrectInputModule();
        }

        public void EnsureCorrectInputModule()
        {
            var es = GetComponent<EventSystem>();
            if (es == null)
                return;

            EnsureSingleEventSystem(es);

            var inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModuleType != null)
            {
                // Project uses Input System package: remove legacy modules.
                var modules = GetComponents<BaseInputModule>();
                foreach (var module in modules)
                {
                    if (module == null)
                        continue;
                    if (module.GetType() == inputSystemModuleType)
                        continue;
                    if (module is Behaviour behaviour)
                        behaviour.enabled = false;
                    DestroyModule(module);
                }

                if (GetComponents<Component>().All(c => c == null || c.GetType() != inputSystemModuleType))
                    gameObject.AddComponent(inputSystemModuleType);

                return;
            }

            // Fallback for legacy input projects.
            if (GetComponent<StandaloneInputModule>() == null)
                gameObject.AddComponent<StandaloneInputModule>();
        }

        static void EnsureSingleEventSystem(EventSystem preferred)
        {
            var all = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (all == null || all.Length <= 1)
                return;

            foreach (var es in all)
            {
                if (es == null || es == preferred)
                    continue;
                DestroyEventSystemObject(es.gameObject);
            }
        }

        void DestroyModule(Component c)
        {
            if (c == null)
                return;

            if (Application.isPlaying)
                Destroy(c);
            else
                DestroyImmediate(c);
        }

        static void DestroyEventSystemObject(GameObject go)
        {
            if (go == null)
                return;

            if (Application.isPlaying)
            {
                go.SetActive(false);
                Destroy(go);
            }
            else
            {
#if UNITY_EDITOR
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    Undo.DestroyObjectImmediate(go);
                else
                    DestroyImmediate(go);
#else
                DestroyImmediate(go);
#endif
            }
        }
    }
}

