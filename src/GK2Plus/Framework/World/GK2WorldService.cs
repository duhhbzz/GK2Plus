using BepInEx.Logging;

namespace GK2Plus.Framework.World
{
    /// <summary>
    /// GK2+ adapter boundary for World.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2WorldService : GK2ServiceBase
    {
        public GK2WorldService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "World";
    }
}
