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
    ///
    /// Automated safety backups use a checkpoint model:
    /// - normal launch/load/save activity creates no GK2+ backup;
    /// - the first Moderate/High mutation after a load/save creates one backup;
    /// - later mutations reuse that checkpoint until GK2 writes/loads a save;
    /// - only a small retained history is kept per slot.
    /// </summary>
    internal sealed class GK2SaveService : GK2ServiceBase
    {
        private const string SaveDataExtension = ".dat";
        private const string SaveInfoExtension = ".info";
        private const string CheatTaintExtension = ".gk2plus-cheat-taint";
        private const string CheatTaintBackupFileName = "GK2Plus-CheatTaint.txt";
        private const int MaxBackupsPerSlot = 5;

        private bool _initialized;
        private bool _saveLoading;
        private bool _saveWriting;

        private long _saveGeneration;
        private long _checkpointGeneration = -1;
        private string _checkpointSlotName;
        private string _checkpointBackupDirectory;

        public GK2SaveService(ManualLogSource logger)
            : base(logger)
        {
        }

        public override string Name => "Saves";

        public string BackupRootPath =>
            Path.Combine(Paths.ConfigPath, "GK2Plus", "SaveBackups");

        public bool IsSaveOperationInProgress => _saveLoading || _saveWriting;

        public int BackupRetentionPerSlot => MaxBackupsPerSlot;

        public bool IsActiveSaveCheatTainted
        {
            get
            {
                return TryGetActiveSlot(
                    out SaveSlotData slotData,
                    out _) &&
                    IsSlotCheatTainted(slotData.slotName);
            }
        }

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
                "GK2+ save safety ready. Persistent mutations use on-demand " +
                $"checkpoints with a {MaxBackupsPerSlot}-backup per-slot retention cap.");
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
            ClearCheckpoint();

            base.Shutdown();
        }

        /// <summary>
        /// Checks active-game/save state and ensures a safety checkpoint exists
        /// before Moderate/High risk mutations.
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
                if (!TryGetOrCreateCheckpoint(
                    operationName,
                    slotData,
                    out SaveBackupResult backupResult))
                {
                    Logger.LogError(
                        $"GK2+ save safety blocked '{operationName}' because " +
                        $"the pre-mutation checkpoint failed: {backupResult.Error}");
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
        /// Requests a normal native GK2 save of the currently loaded slot.
        ///
        /// This deliberately does not create a GK2+ safety checkpoint first:
        /// the requested operation is the save itself, and SaveSystem's normal
        /// write events invalidate any older checkpoint when the write completes.
        /// </summary>
        public bool TryManualSave(out string error)
        {
            error = null;

            if (!TryGetActiveSlot(out SaveSlotData slotData, out error))
            {
                return false;
            }

            if (IsSaveOperationInProgress)
            {
                error = "GK2 is already loading or writing a save.";
                return false;
            }

            MainGame mainGame = MainGame.Instance;
            GameSave gameSave = mainGame?.GameSave;

            if (gameSave == null)
            {
                error = "The active GameSave is unavailable.";
                return false;
            }

            bool callbackReceived = false;
            bool success = false;

            try
            {
                Logger.LogInfo(
                    $"GK2+ manual save requested for slot '{slotData.slotName}'.");

                SaveSystem.Save(
                    slotData,
                    gameSave,
                    callbackSuccessful: () =>
                    {
                        callbackReceived = true;
                        success = true;
                    },
                    callbackUnsuccessful: () =>
                    {
                        callbackReceived = true;
                        success = false;
                    });

                if (!callbackReceived)
                {
                    error = "GK2 SaveSystem returned without a completion callback.";
                    Logger.LogError("GK2+ manual save failed: " + error);
                    return false;
                }

                if (!success)
                {
                    error = "GK2 SaveSystem reported that the save failed.";
                    Logger.LogError("GK2+ manual save failed: " + error);
                    return false;
                }

                Logger.LogInfo(
                    $"GK2+ manual save completed for slot '{slotData.slotName}'.");

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Logger.LogError(
                    $"GK2+ manual save threw an unexpected exception: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Returns the timestamp stored by GK2 for the active slot's most
        /// recent native save. This is updated by GameSave.PrepareToSave(),
        /// regardless of whether the save came from sleep or GK2+ manual save.
        /// </summary>
        public bool TryGetLastSaveDateTime(out DateTime savedAt)
        {
            savedAt = default;

            if (!TryGetActiveSlot(out SaveSlotData slotData, out _))
            {
                return false;
            }

            try
            {
                savedAt = slotData.GetSaveDateTime();
                return savedAt != default;
            }
            catch
            {
                savedAt = default;
                return false;
            }
        }

        /// <summary>
        /// Permanently marks the active save lineage as cheat-tainted.
        /// The marker lives beside GK2's save files without changing the game's
        /// serialized schema. Existing GK2+ backups are marked at the same time.
        /// </summary>
        public bool TryMarkActiveSaveCheatTainted(
            string cheatId,
            out string error)
        {
            error = null;

            if (!TryGetActiveSlot(
                out SaveSlotData slotData,
                out error))
            {
                return false;
            }

            if (IsSaveOperationInProgress)
            {
                error = "GK2 is currently loading or writing a save.";
                return false;
            }

            try
            {
                string markerPath =
                    GetCheatTaintPath(slotData.slotName);

                if (!File.Exists(markerPath))
                {
                    WriteCheatTaintMarker(
                        markerPath,
                        slotData.slotName,
                        cheatId);
                }

                if (!TryMarkExistingBackupsCheatTainted(
                    slotData.slotName,
                    markerPath,
                    out error))
                {
                    Logger.LogError(
                        $"GK2+ cheat taint was written for slot '{slotData.slotName}', " +
                        $"but one or more existing backups could not be marked: {error}");
                    return false;
                }

                Logger.LogWarning(
                    $"GK2+ cheat taint active for slot '{slotData.slotName}'. " +
                    "Platform achievements will be blocked while this save is loaded.");

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;

                Logger.LogError(
                    $"GK2+ failed to mark the active save as cheat-tainted: {ex}");

                return false;
            }
        }

        public bool IsSlotCheatTainted(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName))
            {
                return false;
            }

            try
            {
                return File.Exists(
                    GetCheatTaintPath(slotName));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Explicitly creates a new backup of the active slot.
        ///
        /// Protected mutations do not call this directly; they use the checkpoint
        /// cache so repeated actions do not repeatedly copy the same save files.
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

            return TryCreateBackupCore(reason, slotData, out result);
        }

        private bool TryGetOrCreateCheckpoint(
            string reason,
            SaveSlotData slotData,
            out SaveBackupResult result)
        {
            if (_checkpointGeneration == _saveGeneration &&
                string.Equals(
                    _checkpointSlotName,
                    slotData.slotName,
                    StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(_checkpointBackupDirectory) &&
                Directory.Exists(_checkpointBackupDirectory))
            {
                result = new SaveBackupResult(
                    true,
                    slotData.slotName,
                    _checkpointBackupDirectory,
                    null);

                Logger.LogDebug(
                    $"GK2+ save safety reused checkpoint for slot " +
                    $"'{slotData.slotName}' generation {_saveGeneration}.");

                return true;
            }

            if (!TryCreateBackupCore(reason, slotData, out result))
            {
                return false;
            }

            _checkpointGeneration = _saveGeneration;
            _checkpointSlotName = slotData.slotName;
            _checkpointBackupDirectory = result.BackupDirectory;

            return true;
        }

        private bool TryCreateBackupCore(
            string reason,
            SaveSlotData slotData,
            out SaveBackupResult result)
        {
            result = null;

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

                string error =
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

                // Stream copies directly from disk to disk. GK2+ does not load
                // the full save into a managed byte[] just to create a backup.
                CopyStableFile(
                    dataSource,
                    Path.Combine(tempDirectory, Path.GetFileName(dataSource)));

                CopyStableFile(
                    infoSource,
                    Path.Combine(tempDirectory, Path.GetFileName(infoSource)));

                bool cheatTainted =
                    IsSlotCheatTainted(slotData.slotName);

                if (cheatTainted)
                {
                    CopyStableFile(
                        GetCheatTaintPath(slotData.slotName),
                        Path.Combine(
                            tempDirectory,
                            CheatTaintBackupFileName));
                }

                File.WriteAllText(
                    Path.Combine(tempDirectory, "GK2Plus-Backup.txt"),
                    BuildManifest(
                        slotData.slotName,
                        reason,
                        saveFolder,
                        dataSource,
                        infoSource,
                        cheatTainted),
                    Encoding.UTF8);

                Directory.Move(tempDirectory, finalDirectory);

                PruneOldBackups(slotBackupRoot, finalDirectory);

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

                result = new SaveBackupResult(
                    false,
                    slotData.slotName,
                    null,
                    ex.Message);

                Logger.LogError(
                    $"GK2+ failed to back up slot '{slotData.slotName}': {ex}");

                return false;
            }
        }

        private static void PruneOldBackups(
            string slotBackupRoot,
            string protectedDirectory)
        {
            DirectoryInfo root = new DirectoryInfo(slotBackupRoot);

            DirectoryInfo[] backups = root
                .GetDirectories()
                .Where(directory =>
                    !directory.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(directory => directory.Name, StringComparer.Ordinal)
                .ToArray();

            foreach (DirectoryInfo backup in backups.Skip(MaxBackupsPerSlot))
            {
                if (string.Equals(
                    backup.FullName,
                    protectedDirectory,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TryDeleteDirectory(backup.FullName);
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
            string infoSource,
            bool cheatTainted)
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
            builder.AppendLine(
                "CheatTainted=" + cheatTainted);

            return builder.ToString();
        }

        private string GetCheatTaintPath(string slotName)
        {
            return Path.Combine(
                SaveSystem.SaveFolder,
                slotName + CheatTaintExtension);
        }

        private void WriteCheatTaintMarker(
            string destinationPath,
            string slotName,
            string cheatId)
        {
            string tempPath = destinationPath + ".tmp";

            try
            {
                string contents =
                    "GK2+ Cheat Taint\n" +
                    "================\n" +
                    "FormatVersion=1\n" +
                    "GK2PlusVersion=" + ModInfo.Version + "\n" +
                    "SlotName=" + slotName + "\n" +
                    "FirstCheatId=" + (cheatId ?? string.Empty) + "\n" +
                    "FirstCheatUtc=" + DateTime.UtcNow.ToString("O") + "\n" +
                    "AchievementsDisabled=True\n";

                File.WriteAllText(
                    tempPath,
                    contents,
                    Encoding.UTF8);

                if (File.Exists(destinationPath))
                {
                    File.Delete(tempPath);
                    return;
                }

                File.Move(
                    tempPath,
                    destinationPath);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private bool TryMarkExistingBackupsCheatTainted(
            string slotName,
            string activeMarkerPath,
            out string error)
        {
            error = null;

            string safeSlot =
                SanitizePathPart(slotName, 64);

            string slotBackupRoot =
                Path.Combine(BackupRootPath, safeSlot);

            if (!Directory.Exists(slotBackupRoot))
            {
                return true;
            }

            try
            {
                foreach (DirectoryInfo backup in
                    new DirectoryInfo(slotBackupRoot).GetDirectories())
                {
                    if (backup.Name.EndsWith(
                        ".tmp",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string destination =
                        Path.Combine(
                            backup.FullName,
                            CheatTaintBackupFileName);

                    if (!File.Exists(destination))
                    {
                        CopyStableFile(
                            activeMarkerPath,
                            destination);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
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
                // Best-effort retention cleanup only. A failed prune must not
                // invalidate a newly-created safety backup.
            }
        }

        private void AdvanceSaveGeneration(string reason)
        {
            _saveGeneration++;
            ClearCheckpoint();

            Logger.LogDebug(
                $"GK2+ save checkpoint invalidated after {reason}; " +
                $"generation={_saveGeneration}.");
        }

        private void ClearCheckpoint()
        {
            _checkpointGeneration = -1;
            _checkpointSlotName = null;
            _checkpointBackupDirectory = null;
        }

        private void OnSaveLoadingStarted()
        {
            _saveLoading = true;
            ClearCheckpoint();
        }

        private void OnSaveLoadingEnded()
        {
            _saveLoading = false;
            AdvanceSaveGeneration("save load");
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
            AdvanceSaveGeneration("save write");
        }
    }
}
