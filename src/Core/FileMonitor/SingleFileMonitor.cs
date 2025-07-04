﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core.Resources;

namespace SonarLint.VisualStudio.Core.FileMonitor
{
    /// <summary>
    /// Monitors a single file for all types of change (creation, modification, deletion, rename)
    /// and raises an event for any change.
    /// </summary>
    public interface ISingleFileMonitor : IDisposable
    {
        event EventHandler FileChanged;
        string MonitoredFilePath { get; }
    }

    /// <summary>
    /// Concrete implementation of ISingleFileMonitor
    /// </summary>
    /// <remarks>
    /// Duplicate events can be raised by the lower-level file system watcher class - see
    /// https://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
    /// This wrapper class ensures that duplicate notifications will not be passed on to clients.
    /// However, if notifications occur very close together then some of them may be lost.
    /// In other words, we're removing duplicates but at the cost of potentially losing a few real
    /// events.
    /// </remarks>
    internal sealed class SingleFileMonitor : ISingleFileMonitor
    {
        private readonly IFileInfo monitoredFile;
        private readonly IFileSystemWatcher fileWatcher;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private DateTime lastWriteTime = DateTime.MinValue;

        internal SingleFileMonitor(IFileSystemWatcherFactory factory, IFileSystem fileSystem, string filePathToMonitor, ILogger logger)
        {
            monitoredFile = fileSystem.FileInfo.FromFileName(filePathToMonitor);
            this.fileSystem = fileSystem;
            this.logger = logger.ForVerboseContext(nameof(SingleFileMonitor), monitoredFile.Directory.Name, monitoredFile.Name);

            EnsureDirectoryExists();

            fileWatcher = factory.CreateNew();
            fileWatcher.Path = Path.GetDirectoryName(filePathToMonitor); // NB will throw if the directory does not exist
            fileWatcher.Filter = Path.GetFileName(filePathToMonitor);
            fileWatcher.NotifyFilter =
                NotifyFilters.CreationTime
                | NotifyFilters.LastWrite
                | NotifyFilters.FileName
                | NotifyFilters.DirectoryName;

            fileWatcher.Changed += OnFileChanged;
            fileWatcher.Created += OnFileChanged;
            fileWatcher.Deleted += OnFileChanged;
            fileWatcher.Renamed += OnFileChanged;

        }

        public string MonitoredFilePath => monitoredFile.FullName;

        private EventHandler fileChangedHandlers;
        public event EventHandler FileChanged
        {
            add
            {
                fileChangedHandlers += value;
                fileWatcher.EnableRaisingEvents = true;
            }
            remove
            {
                fileChangedHandlers -= value;
                if (fileChangedHandlers == null)
                {
                    fileWatcher.EnableRaisingEvents = false;
                }
            }
        }

        internal /* for testing */ bool FileWatcherIsRaisingEvents
        {
            get
            {
                return fileWatcher.EnableRaisingEvents;
            }
        }

        private void EnsureDirectoryExists()
        {
            // Exception handling: not much point in catch exceptions here - if we can't
            // create a missing directory then the creation of the file watcher will
            // fail too, so the monitor class won't be constructed correctly.
            if (!monitoredFile.Directory.Exists)
            {
                monitoredFile.Directory.Create();
                logger.WriteLine(Strings.FileMonitor_CreatedDirectory, monitoredFile.DirectoryName);
            }
        }

        private void CleanUpEmptyDirectory()
        {
            try
            {
                if (!monitoredFile.Directory.EnumerateFileSystemInfos().Any())
                {
                    monitoredFile.Directory.Delete(false);
                    logger.WriteLine(Strings.FileMonitor_CleanedUpDirectory, monitoredFile.DirectoryName);
                }
            }
            catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
            {
                logger.LogVerbose(e.Message);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs args)
        {
            Debug.Assert(fileChangedHandlers != null, "Not expecting file system events to be monitored if there are no listeners");
            if (fileChangedHandlers == null || disposedValue)
            {
                return;
            }

            try
            {
                fileWatcher.EnableRaisingEvents = false;

                // We're trying to ignore duplicate events by checking the last-write time.
                // However, the precision of DateTime means that it is possible for separate events
                // that happen very close together to report the same last-write time. If that happens,
                // we'll be ignoring a "real" notification.
                var currentTime = fileSystem.File.GetLastWriteTimeUtc(args.FullPath);

                if (args.ChangeType != WatcherChangeTypes.Renamed && currentTime == lastWriteTime)
                {
                    logger.WriteLine($"Ignoring duplicate change event: {args.ChangeType}");
                    return;
                }

                lastWriteTime = currentTime;
                logger.WriteLine(Strings.FileMonitor_FileChanged, MonitoredFilePath, args.ChangeType);

                fileChangedHandlers(this, EventArgs.Empty);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.FileMonitor_ErrorHandlingFileChange, ex.Message);
            }
            finally
            {
                // Re-check we haven't been disposed on another thread (possible race condition
                // if Dispose is called after the !disposedValue check)
                if (!disposedValue)
                {
                    fileWatcher.EnableRaisingEvents = true;
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;

                if (disposing)
                {
                    fileWatcher.Changed -= OnFileChanged;
                    fileWatcher.Created -= OnFileChanged;
                    fileWatcher.Deleted -= OnFileChanged;
                    fileWatcher.Renamed -= OnFileChanged;
                    fileWatcher.Dispose();
                    fileChangedHandlers = null;
                    CleanUpEmptyDirectory();
                }
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
