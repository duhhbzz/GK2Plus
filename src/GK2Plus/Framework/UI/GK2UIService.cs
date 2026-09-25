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

        public override void Shutdown()
        {
            if (_modMenuController != null)
            {
                _modMenuController.ShutdownController();
            }

            _modMenuController = null;
            _menuActions.Clear();

            base.Shutdown();
        }
    }
}
