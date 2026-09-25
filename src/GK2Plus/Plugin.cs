using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using GK2Plus.Core;
using GK2Plus.Framework;
using GK2Plus.Framework.Diagnostics;
using GK2Plus.Framework.Saves;
using GK2Plus.Framework.UI;
#if DEBUG
using UnityEngine;
#endif

namespace GK2Plus
{
    [BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        private FeatureRegistry _featureRegistry;
        private CompatibilityManager _compatibilityManager;
        private GK2Services _services;

        internal static ConfigEntry<bool> MasterEnabled { get; private set; }

        private void Awake()
        {
            Logger.LogInfo("----------------------------------------");
            Logger.LogInfo($"{ModInfo.Name} v{ModInfo.Version}");
            Logger.LogInfo("Graveyard Keeper Plus");
            Logger.LogInfo("----------------------------------------");

            MasterEnabled = Config.Bind(
                "General",
                "Enabled",
                true,
                "Master switch for GK2+ gameplay features."
            );

            _harmony = new Harmony(ModInfo.Guid);

            _compatibilityManager =
                new CompatibilityManager(Logger);

            _compatibilityManager.Scan();

            _services =
                new GK2Services(Logger);

            _services.Initialize();

            FrameworkDiagnostics.LogReady(Logger);

            // The UI framework service owns the persistent GK2+ menu lifecycle.
            StartCoroutine(MainMenuBadgeController.Run(Logger));

            _featureRegistry =
                new FeatureRegistry();

            RegisterFeatures(_featureRegistry);

            if (MasterEnabled.Value)
            {
                _featureRegistry.InitializeAll(
                    Config,
                    Logger,
                    _harmony
                );
            }
            else
            {
                Logger.LogWarning(
                    "GK2+ master switch is disabled. " +
                    "Gameplay features will not initialize."
                );
            }

            Logger.LogInfo(
                $"GK2+ v{ModInfo.Version} loaded successfully."
            );
        }

#if DEBUG
        private void Update()
        {
            if (_services == null || !Input.GetKeyDown(KeyCode.F9))
            {
                return;
            }

            Logger.LogInfo(
                "[DEV] F9 save-safety probe requested " +
                "(Moderate risk; no game state will be changed)."
            );

            if (_services.Saves.TryPrepareMutation(
                "Dev-Save-Safety-Probe",
                SaveMutationRisk.Moderate,
                out SaveMutationContext context))
            {
                Logger.LogInfo(
                    "[DEV] Save-safety probe passed. " +
                    $"slot='{context.SlotName}', " +
                    $"checkpoint='{context.BackupDirectory}'."
                );
            }
            else
            {
                Logger.LogWarning(
                    "[DEV] Save-safety probe was blocked. " +
                    "Check the preceding save-safety log message for the reason."
                );
            }
        }
#endif

        private static void RegisterFeatures(
            FeatureRegistry registry
        )
        {
            // Gameplay features will be registered here as
            // individual modules.
            //
            // Example:
            // registry.Register(new LargerStacksFeature());
        }

        private void OnDestroy()
        {
            _services?.Shutdown();
            _harmony?.UnpatchSelf();
        }
    }
}
