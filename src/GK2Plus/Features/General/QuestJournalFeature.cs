using System;
using System.Reflection;
using BepInEx.Configuration;
using GK2Plus.Core;
using GK2Plus.Framework.UI;
using HarmonyLib;

namespace GK2Plus.Features.General
{
    /// <summary>
    /// Replaces GK2's quest-tree visualization with a traditional RPG quest
    /// journal while leaving the underlying quest/save systems untouched.
    /// </summary>
    internal sealed class QuestJournalFeature : FeatureBase
    {
        private static QuestJournalFeature _activeInstance;

        private readonly ConfigFile _config;
        private readonly GK2UIService _uiService;

        private GK2QuestJournalController _controller;

        public QuestJournalFeature(
            ConfigFile config,
            GK2UIService uiService)
        {
            _config =
                config ??
                throw new ArgumentNullException(
                    nameof(config));

            _uiService =
                uiService ??
                throw new ArgumentNullException(
                    nameof(uiService));
        }

        public override string Id =>
            "quest-journal";

        public override string Name =>
            "RPG Quest Journal";

        public override string Category =>
            "General";

        public override string Description =>
            "Replaces the native quest-tree page with a traditional RPG-style quest journal.";

        protected override bool DefaultEnabled =>
            true;

        protected override void OnInitialize()
        {
            _activeInstance =
                this;

            _controller =
                new GK2QuestJournalController(
                    Logger);

            _uiService.RegisterFeatureToggleControl(
                new GK2FeatureToggleControl(
                    Id,
                    Category,
                    Name,
                    () => Enabled?.Value ?? DefaultEnabled,
                    BuildStatus,
                    SetEnabledFromMenu,
                    order: 30));

            PatchQuestPage();

            Logger.LogInfo(
                "RPG Quest Journal hooks initialized.");
        }

        private string BuildStatus()
        {
            return Enabled?.Value == true
                ? "ON · replaces native quest tree"
                : "OFF · native quest tree";
        }

        private void SetEnabledFromMenu(
            bool enabled)
        {
            if (Enabled == null)
            {
                return;
            }

            Enabled.Value =
                enabled;

            _config.Save();

            if (!enabled)
            {
                _controller?
                    .RestoreNativeTree();
            }
            else
            {
                _controller?
                    .RefreshIfVisible();
            }

            _uiService.RefreshMenu();
        }

        private void PatchQuestPage()
        {
            Patch(
                typeof(global::QuestTreePageWidget),
                "Display",
                prefixName:
                    nameof(QuestTreeDisplayPrefix));

            Patch(
                typeof(global::QuestTreePageWidget),
                "Hide",
                postfixName:
                    nameof(QuestTreeHidePostfix));

            Patch(
                typeof(global::QuestSystemData),
                "StartQuest",
                postfixName:
                    nameof(QuestStateChangedPostfix));

            Patch(
                typeof(global::QuestSystemData),
                "CompleteQuest",
                postfixName:
                    nameof(QuestStateChangedPostfix));
        }

        private void Patch(
            Type type,
            string methodName,
            string prefixName = null,
            string postfixName = null)
        {
            MethodInfo original =
                AccessTools.Method(
                    type,
                    methodName);

            if (original == null)
            {
                Logger.LogWarning(
                    $"RPG Quest Journal could not find {type.Name}.{methodName}.");
                return;
            }

            HarmonyMethod prefix =
                string.IsNullOrWhiteSpace(
                    prefixName)
                    ? null
                    : new HarmonyMethod(
                        typeof(QuestJournalFeature),
                        prefixName);

            HarmonyMethod postfix =
                string.IsNullOrWhiteSpace(
                    postfixName)
                    ? null
                    : new HarmonyMethod(
                        typeof(QuestJournalFeature),
                        postfixName);

            Harmony.Patch(
                original,
                prefix,
                postfix);
        }

        private static bool QuestTreeDisplayPrefix(
            global::QuestTreePageWidget __instance,
            string focusOnQuest)
        {
            QuestJournalFeature feature =
                _activeInstance;

            if (feature == null ||
                feature.Enabled?.Value != true)
            {
                feature?
                    ._controller?
                    .RestoreNativeTree();

                return true;
            }

            bool shown =
                feature._controller?
                    .TryShow(
                        __instance,
                        focusOnQuest) ==
                true;

            // Fall back to the native tree automatically if the custom view
            // cannot initialize against a future game build.
            return !shown;
        }

        private static void QuestTreeHidePostfix()
        {
            _activeInstance?
                ._controller?
                .Hide();
        }

        private static void QuestStateChangedPostfix()
        {
            _activeInstance?
                ._controller?
                .RefreshIfVisible();
        }
    }
}
