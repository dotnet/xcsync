// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Serilog;

namespace xcsync;

/// <summary>
/// Monitors file changes in the specified project.
/// </summary>
/// <param name="fileSystemWatcher">an instance of a <see cref="FileSystemWatcher"/></param>
/// <param name="logger"></param>
class ProjectFileChangeMonitor (IFileSystem fileSystem, IFileSystemWatcher fileSystemWatcher, ILogger logger) : IDisposable {
	readonly static Action<string> defaultOnFileChanged = _ => { };
	readonly static Action<string, string> defaultOnFileRenamed = (_, _) => { };
	readonly static Action<Exception> defaultOnError = _ => { };

	/// <summary>
	/// Called when a file is changed.
	/// </summary>
	public Action<string> OnFileChanged { get; set; } = defaultOnFileChanged;

	/// <summary>
	/// Called when a file is renamed.
	/// </summary>
	public Action<string, string> OnFileRenamed { get; set; } = defaultOnFileRenamed;

	/// <summary>
	/// Called when an error occurs.
	/// </summary>
	public Action<Exception> OnError { get; set; } = defaultOnError;

	readonly ILogger logger = logger;
	ISyncableProject? project;
	CancellationToken token;

	readonly IFileSystemWatcher watcher = fileSystemWatcher;

	bool disposedValue;

	ExtensionFilter _extensionFilter = new (["."]);

	/// <summary>
	/// Starts monitoring the project file changes.
	/// </summary>
	/// <param name="token"></param>
	public void StartMonitoring (ISyncableProject project, CancellationToken token = default)
	{
		logger.Debug (Strings.Watch.StartMonitoringProject (project.RootPath));
		this.token = token;

		this.project = project;

		watcher.Path = fileSystem.Path.GetDirectoryName (project.RootPath)!;

		watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
		watcher.IncludeSubdirectories = true;

		watcher.Changed += OnChangedHandler;
		watcher.Created += OnChangedHandler;
		watcher.Deleted += OnChangedHandler;
		watcher.Renamed += OnRenamedHandler;
		watcher.Error += OnErrorHandler;

		watcher.EnableRaisingEvents = true;

		_extensionFilter = project.ProjectFilesFilter;

		logger.Debug (Strings.Watch.FileChangeFilter (_extensionFilter.ToString ()!));
	}

	/// <summary>
	/// Stops monitoring the project file changes.
	/// </summary>
	public void StopMonitoring ()
	{
		logger.Debug (Strings.Watch.StartMonitoringProject (project!.RootPath));

		watcher.EnableRaisingEvents = false;

		watcher.Changed -= OnChangedHandler;
		watcher.Created -= OnChangedHandler;
		watcher.Deleted -= OnChangedHandler;
		watcher.Renamed -= OnRenamedHandler;
		watcher.Error -= OnErrorHandler;
	}

	protected virtual void Dispose (bool disposing)
	{
		if (!disposedValue) {
			if (disposing) {
				StopMonitoring ();
				watcher.Dispose ();

				OnFileChanged = defaultOnFileChanged;
				OnFileRenamed = defaultOnFileRenamed;
				OnError = defaultOnError;
			}

			disposedValue = true;
		}
	}

	void OnRenamedHandler (object sender, RenamedEventArgs e)
	{
		if (disposedValue)
			return;

		if (token.IsCancellationRequested) {
			StopMonitoring ();
			return;
		}

		if (!_extensionFilter.ProcessRenameEvent (e.OldFullPath, e.FullPath))
			return;

		logger.Information (Strings.Watch.FileRenamed (e.OldFullPath, e.FullPath, project!.Name));

		OnFileRenamed (e.OldFullPath, e.FullPath);
	}

	void OnChangedHandler (object sender, FileSystemEventArgs e)
	{
		if (disposedValue)
			return;

		if (token.IsCancellationRequested) {
			StopMonitoring ();
			return;
		}

		if (!_extensionFilter.ProcessEvent (e.FullPath))
			return;

		logger.Information (Strings.Watch.FileChanged (e.FullPath, project!.Name));

		OnFileChanged (e.FullPath);
	}

	void OnErrorHandler (object sender, ErrorEventArgs e)
	{
		if (disposedValue)
			return;

		if (token.IsCancellationRequested) {
			StopMonitoring ();
			return;
		}

		var ex = e.GetException ();
		logger.Error (ex, Strings.Watch.ErrorWhileMonitoring (project!.RootPath));

		OnError (ex);

		Dispose ();
	}

	public void Dispose ()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose (disposing: true);
		GC.SuppressFinalize (this);
	}
}
