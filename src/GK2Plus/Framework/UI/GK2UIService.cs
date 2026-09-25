using BepInEx.Logging;

namespace GK2Plus.Framework.UI
{
    /// <summary>
    /// GK2+ adapter boundary for UI.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2UIService : GK2ServiceBase
    {
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
            Logger.LogInfo("GK2+ UI service owns the persistent mod-menu controller.");
        }

        public override void Shutdown()
        {
            _modMenuController?.ShutdownController();
            _modMenuController = null;

            base.Shutdown();
        }
    }
}
