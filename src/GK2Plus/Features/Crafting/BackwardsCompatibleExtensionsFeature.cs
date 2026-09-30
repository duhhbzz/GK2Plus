using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using GK2Plus.Core;
using GK2Plus.Framework.UI;
using HarmonyLib;

namespace GK2Plus.Features.Crafting
{
    /// <summary>
    /// Lets selected upgraded workbench extensions satisfy recipes that require
    /// their lower-tier predecessor. The vanilla extension/linking system remains
    /// authoritative; GK2+ only relaxes the final recipe eligibility check.
    /// </summary>
    internal sealed class BackwardsCompatibleExtensionsFeature : FeatureBase
    {
        private const string ToolRack = "tool_rack";
        private const string FineToolRack = "tool_rack_fine";

        private static BackwardsCompatibleExtensionsFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;

        private static readonly Dictionary<string, HashSet<string>> CompatibleUpgrades =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal)
            {
                {
                    ToolRack,
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        FineToolRack
                    }
                }
            };

        internal BackwardsCompatibleExtensionsFeature(
            ConfigFile config,
            GK2UIService uiService)
        {
            _config = config ??
                throw new ArgumentNullException(nameof(config));

            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
        }

        public override string Id => "backwards-compatible-extensions";

        public override string Name => "Backwards Compatible Extensions";

        public override string Category => "Crafting";

        public override string Description =>
            "Allow supported upgraded workbench extensions to satisfy recipes that require their lower-tier predecessor.";

        protected override bool DefaultEnabled => false;

        protected override void OnInitialize()
        {
            _activeInstance = this;

            var method = AccessTools.Method(
                typeof(global::CraftComponent),
                "IsCraftAllowedByAttachedExtensions",
                new[] { typeof(global::CraftDefBase) });

            if (method == null)
            {
                Logger.LogError(
                    "Backwards Compatible Extensions could not resolve " +
                    "CraftComponent.IsCraftAllowedByAttachedExtensions. " +
                    "The feature will remain inactive.");
            }
            else
            {
                Harmony.Patch(
                    method,
                    postfix: new HarmonyMethod(
                        typeof(BackwardsCompatibleExtensionsFeature),
                        nameof(IsCraftAllowedPostfix)));
            }

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    "Backwards Compatible Extensions",
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildUiStatus,
                    SetEnabledFromMainMenu,
                    order: 100));
        }

        protected override void OnEnabled()
        {
            Logger.LogInfo(
                "Backwards Compatible Extensions enabled: " +
                "Fine Tool Rack can satisfy recipes requiring Tool Rack.");
        }

        private string BuildUiStatus()
        {
            return Enabled?.Value == true
                ? "ON"
                : "OFF";
        }

        private void SetEnabledFromMainMenu(bool enabled)
        {
            if (Enabled == null)
            {
                return;
            }

            if (Enabled.Value == enabled)
            {
                _uiService.RefreshMenu();
                return;
            }

            Enabled.Value = enabled;
            _config.Save();

            Logger.LogInfo(
                $"Backwards Compatible Extensions {(enabled ? "enabled" : "disabled")} from the main menu.");

            _uiService.RefreshMenu();
        }

        private static void IsCraftAllowedPostfix(
            global::CraftComponent __instance,
            global::CraftDefBase craftDef,
            ref bool __result)
        {
            if (__result ||
                _activeInstance?.Enabled?.Value != true ||
                __instance == null ||
                craftDef == null ||
                string.IsNullOrEmpty(craftDef.extensionNeedId))
            {
                return;
            }

            if (!CompatibleUpgrades.TryGetValue(
                    craftDef.extensionNeedId,
                    out HashSet<string> compatibleUpgradeIds))
            {
                return;
            }

            global::WgoData workbench =
                __instance.CraftableObject as global::WgoData;

            if (workbench == null ||
                workbench.Definition == null ||
                workbench.AttachedWorkbenchExtensions == null ||
                workbench.AttachedWorkbenchExtensions.Count == 0)
            {
                return;
            }

            global::MainGame mainGame =
                global::MainGame.Instance;

            if (mainGame?.GameSave?.WorldData == null)
            {
                return;
            }

            List<string> allowedExtensions =
                workbench.Definition.attachedWorkbenchExtensionIds;

            foreach (global::SGuid extensionGuid in
                     workbench.AttachedWorkbenchExtensions)
            {
                global::WgoData extension =
                    mainGame.GameSave.WorldData.GetWgoData(
                        extensionGuid);

                if (extension == null ||
                    string.IsNullOrEmpty(extension.id) ||
                    !compatibleUpgradeIds.Contains(extension.id))
                {
                    continue;
                }

                // Preserve the vanilla parent/extension relationship. An upgraded
                // extension only substitutes when this workbench already accepts
                // that upgraded extension normally.
                if (allowedExtensions != null &&
                    allowedExtensions.Contains(extension.id))
                {
                    __result = true;

                    _activeInstance.Logger.LogDebug(
                        $"Backwards Compatible Extensions accepted '{extension.id}' " +
                        $"for recipe '{craftDef.id}' requiring '{craftDef.extensionNeedId}' " +
                        $"on workbench '{workbench.id}'.");

                    return;
                }
            }
        }
    }
}
