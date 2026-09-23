using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using GK2Plus.Core;

namespace GK2Plus
{
    [BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        private FeatureRegistry _featureRegistry;
        private CompatibilityManager _compatibilityManager;

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
            _harmony?.UnpatchSelf();
        }
    }
}
