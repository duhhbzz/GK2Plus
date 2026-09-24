using BepInEx.Logging;

namespace GK2Plus.Framework.Saves
{
    /// <summary>
    /// GK2+ adapter boundary for Saves.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2SaveService : GK2ServiceBase
    {
        public GK2SaveService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Saves";
    }
}
