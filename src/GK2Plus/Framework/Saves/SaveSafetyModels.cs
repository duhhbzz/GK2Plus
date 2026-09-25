using System;

namespace GK2Plus.Framework.Saves
{
    /// <summary>
    /// Risk classification for operations that can alter the active game state.
    ///
    /// Low: transient/session behavior; no automatic save backup.
    /// Moderate: persistent player/economy state; backup required.
    /// High: progression/world state; backup required.
    /// </summary>
    internal enum SaveMutationRisk
    {
        Low = 0,
        Moderate = 1,
        High = 2
    }

    internal sealed class SaveBackupResult
    {
        public SaveBackupResult(
            bool success,
            string slotName,
            string backupDirectory,
            string error)
        {
            Success = success;
            SlotName = slotName;
            BackupDirectory = backupDirectory;
            Error = error;
        }

        public bool Success { get; }

        public string SlotName { get; }

        public string BackupDirectory { get; }

        public string Error { get; }
    }

    internal sealed class SaveMutationContext
    {
        public SaveMutationContext(
            string operationName,
            SaveMutationRisk risk,
            string slotName,
            DateTime preparedUtc,
            string backupDirectory)
        {
            OperationName = operationName;
            Risk = risk;
            SlotName = slotName;
            PreparedUtc = preparedUtc;
            BackupDirectory = backupDirectory;
        }

        public string OperationName { get; }

        public SaveMutationRisk Risk { get; }

        public string SlotName { get; }

        public DateTime PreparedUtc { get; }

        public string BackupDirectory { get; }

        public bool HasBackup => !string.IsNullOrEmpty(BackupDirectory);
    }
}
