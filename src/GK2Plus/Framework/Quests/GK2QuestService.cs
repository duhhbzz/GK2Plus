using BepInEx.Logging;

namespace GK2Plus.Framework.Quests
{
    /// <summary>
    /// GK2+ adapter boundary for Quests.
    ///
    /// Keep direct game-internal access inside this layer rather than feature modules.
    /// </summary>
    internal sealed class GK2QuestService : GK2ServiceBase
    {
        public GK2QuestService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Quests";
    }
}
