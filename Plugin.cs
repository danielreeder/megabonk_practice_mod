using System;
using Assets.Scripts.Actors.Enemies;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.UI;

namespace Shrine_Counter
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Plugin : BasePlugin
    {
        public const string
            MODNAME = "Shrine_Counter",
            AUTHOR = "BedlessSleeper",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "0.1.4";

        internal static ManualLogSource log;
        private static GameObject EnemyCounter;

        public static int EnemyCount { get; private set; } = 0;
        public static bool IsRoundActive { get; private set; } = false;

        public Plugin() => log = Log;

        public override void Load()
        {
            log.LogInfo("Shrine Counter Mod loaded!");

            // Register GUI type
            ClassInjector.RegisterTypeInIl2Cpp<EnemyCounterGUI>();

            // Patch all Harmony methods
            new Harmony(GUID).PatchAll();

            // Create the GUI
            CreateGUI();
        }

        private static void CreateGUI()
        {
            if (EnemyCounter != null) return;

            EnemyCounter = new GameObject("EnemyCounterGUI");
            EnemyCounter.AddComponent<EnemyCounterGUI>();
            UnityEngine.Object.DontDestroyOnLoad(EnemyCounter);
        }

        public static void SafeCleanup()
        {
            try
            {
                if (EnemyCounter != null)
                {
                    UnityEngine.Object.Destroy(EnemyCounter);
                    EnemyCounter = null;
                    //log?.LogInfo("[EnemyCounter] GUI cleaned up.");
                }

                EnemyCount = 0;
                IsRoundActive = false;
            }
            catch (Exception ex)
            {
                log?.LogError($"[EnemyCounter] Cleanup failed: {ex}");
            }
        }

        // --- Harmony Patches --- Enempy Spawn ==> Enemy Count +1

        [HarmonyPatch]
        public static class EnemyManagerSpawnEnemyIntPatch
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("EnemyManager");
                return AccessTools.Method(type, "SpawnEnemy", [typeof(EnemyData), typeof(int), typeof(bool), typeof(EEnemyFlag), typeof(bool)]);
            }

            //Counts shrine shit here
            static void Postfix() => EnemyCount++;
        }

        [HarmonyPatch]
        public static class EnemyManagerSpawnEnemyVecPatch
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("EnemyManager");
                return AccessTools.Method(type, "SpawnEnemy", [typeof(EnemyData), typeof(Vector3), typeof(int), typeof(bool), typeof(EEnemyFlag), typeof(bool)]);
            }

            //Counts shrine shit here
            static void Postfix() => EnemyCount++;
        }

        [HarmonyPatch]
        public static class EnemyManagerRemoveEnemyPatch
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("EnemyManager");
                return AccessTools.Method(type, "RemoveEnemy");
            }

            static void Postfix() => EnemyCount--;
        }

        [HarmonyPatch]
        public static class GameManager_StartPatch
        {
            static System.Reflection.MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("GameManager");
                return AccessTools.Method(type, "Start");
            }

            static void Postfix()
            {
                EnemyCount = 0;
                IsRoundActive = true;
            }
        }

        [HarmonyPatch(typeof(GameManager), "OnDied")]
        public static class GameManager_OnDiedPatch
        {
            static void Postfix()
            {
                IsRoundActive = false;
                EnemyCount = 0;
            }
        }

        [HarmonyPatch(typeof(GameManager), "OnDestroy")]
        public static class GameManager_OnDestroyPatch
        {
            static void Postfix()
            {
                IsRoundActive = false;
                EnemyCount = 0;
            }
        }

        // --- GUI Class --- AHHHHHHHHHHHHHH
        public class EnemyCounterGUI : MonoBehaviour
        {
            private Text textObj;
            private Canvas canvas;

            private int lastCount = -1;
            private bool lastRoundActive = false;

            void Awake() => DontDestroyOnLoad(gameObject);

            void Start()
            {
                // Create a new canvas
                GameObject canvasGO = new GameObject("EnemyCounterCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                UnityEngine.Object.DontDestroyOnLoad(canvasGO);

                // Create the text object
                GameObject textGO = new GameObject("EnemyCounterText");
                textGO.transform.SetParent(canvasGO.transform, false);

                textObj = textGO.AddComponent<Text>();
                textObj.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textObj.alignment = TextAnchor.UpperLeft; // anchor top-left
                textObj.horizontalOverflow = HorizontalWrapMode.Overflow;
                textObj.verticalOverflow = VerticalWrapMode.Overflow;

                // Add shadow for readability
                var shadow = textGO.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
                shadow.effectDistance = new Vector2(2, -2);

                UpdateTextPositionAndSize(); // set initial position and font size
                textObj.text = $"Enemies Alive: {Plugin.EnemyCount}";
            }

            void Update()
            {
                if (textObj == null) return;

                // Update text when values change
                if (Plugin.EnemyCount != lastCount || Plugin.IsRoundActive != lastRoundActive)
                {
                    textObj.enabled = Plugin.IsRoundActive;
                    textObj.text = $"Enemies Alive: {Plugin.EnemyCount}";

                    lastCount = Plugin.EnemyCount;
                    lastRoundActive = Plugin.IsRoundActive;
                }

                // Dynamically adjust position and font size every frame in case of resolution changes
                UpdateTextPositionAndSize();
            }

            private void UpdateTextPositionAndSize()
            {
                if (textObj == null) return;

                // Anchor top-left
                textObj.rectTransform.anchorMin = new Vector2(0f, 1f);
                textObj.rectTransform.anchorMax = new Vector2(0f, 1f);
                textObj.rectTransform.pivot = new Vector2(0f, 1f);

                // Position: offset as % of screen size
                float horizontalOffset = Screen.width * 0.64f; // % Of Monitor -Lower Left Higher Right
                float verticalOffset = Screen.height * 0.039f; // % Of Monitor -Lower Up Higher Down
                textObj.rectTransform.anchoredPosition = new Vector2(horizontalOffset, -verticalOffset);

                // Font size scales with screen height
                textObj.fontSize = Mathf.RoundToInt(Screen.height * 0.025f); // 2.5% of screen height
            }

            void OnDisable() => Plugin.SafeCleanup();
            void OnDestroy() => Plugin.SafeCleanup();
        }
    }
}