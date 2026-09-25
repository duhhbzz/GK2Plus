using System;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using GK2Plus.Core;

namespace GK2Plus.Framework.Saves
{
    /// <summary>
    /// GK2+ adapter boundary for saves and guarded persistent mutations.
    ///
    /// Feature modules should use this service before changing persistent
    /// player/progression/world state instead of touching save files directly.
    /// Backups are strictly on-demand; normal game launches/saves do not create
    /// GK2+ backup files.
    /// </summary>
    internal sealed class GK2SaveService : GK2ServiceBase
    {
        private const string SaveDataExtension = ".dat";
        private const string SaveInfoExtension = ".info";

        private bool _initialized;
        private bool _saveLoading;
        private bool _saveWriting;

        public GK2SaveService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Saves";

        public string BackupRootPath =>
            Path.Combine(Paths.ConfigPath, "GK2Plus", "SaveBackups");

        public bool IsSaveOperationInProgress => _saveLoading || _saveWriting;

        public bool HasLoadedSave
        {
            get
            {
                try
                {
                    return MainGame.Instance != null &&
                           MainGame.Instance.gameState == MainGame.GameState.InGame &&
                           MainGame.Instance.GameSave != null &&
                           MainGame.Instance.SaveSlotData != null &&
                           !string.IsNullOrWhiteSpace(MainGame.Instance.SaveSlotData.slotName);
                }
                catch
                {
                    return false;
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (_initialized)
            {
                return;
            }

            SaveSystem.OnSaveLoadingStarted += OnSaveLoadingStarted;
            SaveSystem.OnSaveLoadingEnded += OnSaveLoadingEnded;
            SaveSystem.OnSaveWriteStarted += OnSaveWriteStarted;
            SaveSystem.OnSaveWriteStartedInstant += OnSaveWriteStartedInstant;
            SaveSystem.OnSaveWriteEnded += OnSaveWriteEnded;

            _initialized = true;

            Logger.LogInfo(
                "GK2+ save safety ready. Persistent mutations can request " +
                "on-demand backups before changing the active save.");
        }

        public override void Shutdown()
        {
            if (_initialized)
            {
                SaveSystem.OnSaveLoadingStarted -= OnSaveLoadingStarted;
                SaveSystem.OnSaveLoadingEnded -= OnSaveLoadingEnded;
                SaveSystem.OnSaveWriteStarted -= OnSaveWriteStarted;
                SaveSystem.OnSaveWriteStartedInstant -= OnSaveWriteStartedInstant;
                SaveSystem.OnSaveWriteEnded -= OnSaveWriteEnded;

                _initialized = false;
            }

            _saveLoading = false;
            _saveWriting = false;

            base.Shutdown();
        }

        /// <summary>
        /// Checks the active-game/save state and creates a point-in-time backup
        /// for Moderate/High risk mutations.
        /// </summary>
        public bool TryPrepareMutation(
            string operationName,
            SaveMutationRisk risk,
            out SaveMutationContext context)
        {
            context = null;

            if (string.IsNullOrWhiteSpace(operationName))
            {
                Logger.LogError("GK2+ save safety rejected an unnamed mutation.");
                return false;
            }

            if (!TryGetActiveSlot(out SaveSlotData slotData, out string error))
            {
                Logger.LogWarning(
                    $"GK2+ save safety blocked '{operationName}': {error}");
                return false;
            }

            if (IsSaveOperationInProgress)
            {
                Logger.LogWarning(
                    $"GK2+ save safety blocked '{operationName}' because " +
                    "GK2 is currently loading or writing a save.");
                return false;
            }

            string backupDirectory = null;

            if (risk >= SaveMutationRisk.Moderate)
            {
                if (!TryCreateBackup(
                    operationName,
                    out SaveBackupResult backupResult))
                {
                    Logger.LogError(
                        $"GK2+ save safety blocked '{operationName}' because " +
                        $"the pre-mutation backup failed: {backupResult.Error}");
                    return false;
                }

                backupDirectory = backupResult.BackupDirectory;
            }

            context = new SaveMutationContext(
                operationName,
                risk,
                slotData.slotName,
                DateTime.UtcNow,
                backupDirectory);

            Logger.LogInfo(
                $"GK2+ mutation prepared: operation='{operationName}', " +
                $"risk={risk}, slot='{slotData.slotName}', " +
                $"backup={(backupDirectory ?? "<not required>")}.");

            return true;
        }

        /// <summary>
        /// Runs a mutation only after the save-safety gate succeeds.
        /// This service does not auto-restore on validation failure; it reports
        /// the backup path so recovery remains explicit and user-controlled.
        /// </summary>
        public bool TryRunProtectedMutation(
            string operationName,
            SaveMutationRisk risk,
            Action mutation,
            Func<bool> validator = null)
        {
            if (mutation == null)
            {
                throw new ArgumentNullException(nameof(mutation));
            }

            if (!TryPrepareMutation(operationName, risk, out SaveMutationContext context))
            {
                return false;
            }

            try
            {
                mutation();

                if (validator != null && !validator())
                {
                    Logger.LogError(
                        $"GK2+ mutation validation failed: operation='{operationName}', " +
                        $"risk={risk}, backup={(context.BackupDirectory ?? "<none>")}.");
                    return false;
                }

                Logger.LogInfo(
                    $"GK2+ mutation completed: operation='{operationName}', " +
                    $"risk={risk}, slot='{context.SlotName}'.");

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    $"GK2+ mutation failed: operation='{operationName}', " +
                    $"risk={risk}, backup={(context.BackupDirectory ?? "<none>")}, " +
                    $"exception={ex}");

                return false;
            }
        }

        /// <summary>
        /// Creates a backup of the active slot's .dat and .info files.
        /// No backup is created merely by initializing GK2+ or saving normally.
        /// </summary>
        public bool TryCreateBackup(
            string reason,
            out SaveBackupResult result)
        {
            result = null;

            if (!TryGetActiveSlot(out SaveSlotData slotData, out string error))
            {
                result = new SaveBackupResult(
                    false,
                    null,
                    null,
                    error);
                return false;
            }

            if (IsSaveOperationInProgress)
            {
                error = "GK2 is currently loading or writing a save.";
                result = new SaveBackupResult(
                    false,
                    slotData.slotName,
                    null,
                    error);
                return false;
            }

            string saveFolder = SaveSystem.SaveFolder;
            string dataSource = Path.Combine(
                saveFolder,
                slotData.slotName + SaveDataExtension);
            string infoSource = Path.Combine(
                saveFolder,
                slotData.slotName + SaveInfoExtension);

            if (!File.Exists(dataSource) || !File.Exists(infoSource))
            {
                string missing = string.Join(
                    ", ",
                    new[]
                    {
                        !File.Exists(dataSource)
                            ? Path.GetFileName(dataSource)
                            : null,
                        !File.Exists(infoSource)
                            ? Path.GetFileName(infoSource)
                            : null
                    }.Where(value => value != null));

                error =
                    $"Required active-slot file(s) are missing: {missing}. " +
                    $"Save folder: {saveFolder}";

                result = new SaveBackupResult(
                    false,
                    slotData.slotName,
                    null,
                    error);

                return false;
            }

            string safeSlot = SanitizePathPart(slotData.slotName, 64);
            string safeReason = SanitizePathPart(
                string.IsNullOrWhiteSpace(reason) ? "manual" : reason,
                48);

            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
            string slotBackupRoot = Path.Combine(BackupRootPath, safeSlot);
            string finalDirectory = Path.Combine(
                slotBackupRoot,
                $"{stamp}_{safeReason}");
            string tempDirectory = finalDirectory + ".tmp";

            try
            {
                Directory.CreateDirectory(slotBackupRoot);

                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }

                Directory.CreateDirectory(tempDirectory);

                CopyStableFile(
                    dataSource,
                    Path.Combine(tempDirectory, Path.GetFileName(dataSource)));

                CopyStableFile(
                    infoSource,
                    Path.Combine(tempDirectory, Path.GetFileName(infoSource)));

                File.WriteAllText(
                    Path.Combine(tempDirectory, "GK2Plus-Backup.txt"),
                    BuildManifest(
                        slotData.slotName,
                        reason,
                        saveFolder,
                        dataSource,
                        infoSource),
                    Encoding.UTF8);

                Directory.Move(tempDirectory, finalDirectory);

                result = new SaveBackupResult(
                    true,
                    slotData.slotName,
                    finalDirectory,
                    null);

                Logger.LogWarning(
                    $"GK2+ save backup created for slot '{slotData.slotName}': " +
                    finalDirectory);

                return true;
            }
            catch (Exception ex)
            {
                TryDeleteDirectory(tempDirectory);

                error = ex.Message;
                result = new SaveBackupResult(
                    false,
                    slotData.slotName,
                    null,
                    error);

                Logger.LogError(
                    $"GK2+ failed to back up slot '{slotData.slotName}': {ex}");

                return false;
            }
        }

        private bool TryGetActiveSlot(
            out SaveSlotData slotData,
            out string error)
        {
            slotData = null;
            error = null;

            try
            {
                MainGame mainGame = MainGame.Instance;

                if (mainGame == null)
                {
                    error = "MainGame is not initialized.";
                    return false;
                }

                if (mainGame.gameState != MainGame.GameState.InGame)
                {
                    error = "No gameplay save is currently loaded.";
                    return false;
                }

                if (mainGame.GameSave == null)
                {
                    error = "The active GameSave is unavailable.";
                    return false;
                }

                slotData = mainGame.SaveSlotData;

                if (slotData == null ||
                    string.IsNullOrWhiteSpace(slotData.slotName))
                {
                    error = "The active save slot is unavailable.";
                    slotData = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = "Failed to resolve the active save slot: " + ex.Message;
                slotData = null;
                return false;
            }
        }

        private static void CopyStableFile(
            string sourcePath,
            string destinationPath)
        {
            long sourceLength;

            using (FileStream source = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete))
            {
                sourceLength = source.Length;

                using (FileStream destination = new FileStream(
                    destinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    source.CopyTo(destination);
                    destination.Flush();
                }
            }

            FileInfo copied = new FileInfo(destinationPath);

            if (!copied.Exists || copied.Length != sourceLength)
            {
                throw new IOException(
                    $"Backup verification failed for '{Path.GetFileName(sourcePath)}'. " +
                    $"Expected {sourceLength} bytes, copied " +
                    $"{(copied.Exists ? copied.Length : 0)} bytes.");
            }
        }

        private static string BuildManifest(
            string slotName,
            string reason,
            string saveFolder,
            string dataSource,
            string infoSource)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("GK2+ Save Safety Backup");
            builder.AppendLine("=======================");
            builder.AppendLine("GK2PlusVersion=" + ModInfo.Version);
            builder.AppendLine("CreatedUtc=" + DateTime.UtcNow.ToString("O"));
            builder.AppendLine("SlotName=" + slotName);
            builder.AppendLine("Reason=" + (reason ?? string.Empty));
            builder.AppendLine("SourceSaveFolder=" + saveFolder);
            builder.AppendLine(
                "DataFile=" + Path.GetFileName(dataSource));
            builder.AppendLine(
                "InfoFile=" + Path.GetFileName(infoSource));

            return builder.ToString();
        }

        private static string SanitizePathPart(
            string value,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);

            foreach (char ch in value)
            {
                if (invalid.Contains(ch) ||
                    char.IsControl(ch) ||
                    ch == Path.DirectorySeparatorChar ||
                    ch == Path.AltDirectorySeparatorChar)
                {
                    builder.Append('_');
                }
                else if (char.IsWhiteSpace(ch))
                {
                    builder.Append('-');
                }
                else
                {
                    builder.Append(ch);
                }

                if (builder.Length >= maxLength)
                {
                    break;
                }
            }

            string sanitized = builder
                .ToString()
                .Trim(' ', '.', '-', '_');

            return string.IsNullOrEmpty(sanitized)
                ? "unnamed"
                : sanitized;
        }

        private static void TryDeleteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) ||
                !Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                // Best-effort cleanup only; preserve the original backup error.
            }
        }

        private void OnSaveLoadingStarted()
        {
            _saveLoading = true;
        }

        private void OnSaveLoadingEnded()
        {
            _saveLoading = false;
        }

        private void OnSaveWriteStarted()
        {
            _saveWriting = true;
        }

        private void OnSaveWriteStartedInstant()
        {
            _saveWriting = true;
        }

        private void OnSaveWriteEnded()
        {
            _saveWriting = false;
        }
    }
}
