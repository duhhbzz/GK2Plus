using BepInEx.Logging;

namespace GK2Plus.Framework.Events
{
    /// <summary>
    /// GK2+ adapter boundary for Events.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2EventService : GK2ServiceBase
    {
        public GK2EventService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Events";
    }
}
