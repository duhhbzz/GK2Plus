using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using GK2Plus.Core;
using GK2Plus.Features.Cheats;
using GK2Plus.Features.General;
using GK2Plus.Features.Inventory;
using GK2Plus.Framework;
using GK2Plus.Framework.Diagnostics;
using GK2Plus.Framework.UI;

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


        private void RegisterFeatures(
            FeatureRegistry registry
        )
        {
            registry.Register(
                new ManualSaveFeature(_services.Saves)
            );

            registry.Register(
                new BasicCheatsFeature(
                    _services.Saves,
                    _services.UI)
            );

            registry.Register(
                new StackSizesFeature(Config)
            );
        }

        private void OnDestroy()
        {
            _services?.Shutdown();
            _harmony?.UnpatchSelf();
        }
    }
}
