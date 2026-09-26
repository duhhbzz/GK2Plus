using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// GK2+ adapter boundary for UI.
    ///
    /// Feature modules register player-facing menu actions here instead of
    /// reaching into ModMenuController directly.
    /// </summary>
    internal sealed class GK2UIService : GK2ServiceBase
    {
        private readonly List<GK2MenuAction> _menuActions =
            new List<GK2MenuAction>();

        private readonly Dictionary<string, System.Func<string>> _tabNotices =
            new Dictionary<string, System.Func<string>>();

        private readonly List<GK2FeatureToggleControl> _featureToggleControls =
            new List<GK2FeatureToggleControl>();

        private readonly List<GK2FeatureOptionControl> _featureOptionControls =
            new List<GK2FeatureOptionControl>();

        private ModMenuController _modMenuController;

        public GK2UIService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "UI";

        public override void Initialize()
        {
            base.Initialize();

            _modMenuController = ModMenuController.Create(Logger);

            foreach (GK2MenuAction action in _menuActions)
            {
                _modMenuController.RegisterMenuAction(action);
            }

            foreach (var notice in _tabNotices)
            {
                _modMenuController.RegisterTabNotice(
                    notice.Key,
                    notice.Value);
            }

            foreach (GK2FeatureToggleControl control in _featureToggleControls)
            {
                _modMenuController.RegisterFeatureToggleControl(control);
            }

            foreach (GK2FeatureOptionControl control in _featureOptionControls)
            {
                _modMenuController.RegisterFeatureOptionControl(control);
            }

            Logger.LogInfo(
                "GK2+ UI service owns the persistent mod-menu controller.");
        }

        public void RegisterMenuAction(GK2MenuAction action)
        {
            if (action == null)
            {
                return;
            }

            _menuActions.Add(action);

            if (_modMenuController != null)
            {
                _modMenuController.RegisterMenuAction(action);
            }
        }

        public void RegisterTabNotice(
            string tab,
            Func<string> noticeProvider)
        {
            if (string.IsNullOrWhiteSpace(tab) ||
                noticeProvider == null)
            {
                return;
            }

            _tabNotices[tab] = noticeProvider;

            if (_modMenuController != null)
            {
                _modMenuController.RegisterTabNotice(
                    tab,
                    noticeProvider);
            }
        }

        public void RegisterFeatureToggleControl(
            GK2FeatureToggleControl control)
        {
            if (control == null)
            {
                return;
            }

            _featureToggleControls.RemoveAll(existing =>
                string.Equals(
                    existing.Id,
                    control.Id,
                    StringComparison.OrdinalIgnoreCase));

            _featureToggleControls.Add(control);

            if (_modMenuController != null)
            {
                _modMenuController.RegisterFeatureToggleControl(
                    control);
            }
        }

        public void RegisterFeatureOptionControl(
            GK2FeatureOptionControl control)
        {
            if (control == null)
            {
                return;
            }

            _featureOptionControls.RemoveAll(existing =>
                string.Equals(
                    existing.Id,
                    control.Id,
                    StringComparison.OrdinalIgnoreCase));

            _featureOptionControls.Add(control);

            if (_modMenuController != null)
            {
                _modMenuController.RegisterFeatureOptionControl(
                    control);
            }
        }

        public void RefreshMenu()
        {
            _modMenuController?.RefreshActiveTab();
        }

        public void HideMenu()
        {
            _modMenuController?.HideMenu();
        }

        public void ShowMenu()
        {
            _modMenuController?.ShowMenu();
        }

        public override void Shutdown()
        {
            if (_modMenuController != null)
            {
                _modMenuController.ShutdownController();
            }

            _modMenuController = null;
            _menuActions.Clear();
            _tabNotices.Clear();
            _featureToggleControls.Clear();
            _featureOptionControls.Clear();

            base.Shutdown();
        }
    }
}
