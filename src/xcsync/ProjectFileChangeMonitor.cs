// Copyright (c) Microsoft Corporation. All rights reserved.

using Serilog;

namespace xcsync;

class ProjectFileChangeMonitor (ISyncableProject project, ILogger logger) : IDisposable {
	readonly ILogger Logger = logger;
	readonly ISyncableProject Project = project;

	readonly FileSystemWatcher watcher = new () {
		Path = project.RootPath,
		NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
		Filter = project.ProjectFilesFilter
	};

	bool disposedValue;

	public void StartMonitoring (CancellationToken token = default)
	{
		Logger.Information ("Monitoring project file changes...");
		watcher.Changed += OnChanged;
		watcher.Created += OnChanged;
		watcher.Deleted += OnChanged;
		watcher.Renamed += OnRenamed;

		watcher.EnableRaisingEvents = true;
	}

	protected virtual void Dispose (bool disposing)
	{
		if (!disposedValue) {
			if (disposing) {
				// TODO: dispose managed state (managed objects)
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			watcher.Changed -= OnChanged;
			watcher.Created -= OnChanged;
			watcher.Deleted -= OnChanged;
			watcher.Renamed -= OnRenamed;

			watcher.EnableRaisingEvents = false;
			watcher.Dispose ();

			disposedValue = true;
		}
	}

	void OnRenamed (object sender, RenamedEventArgs e)
	{
		Logger.Information (string.Format ("File renamed from {0} to {1} in {2} project.", e.OldFullPath, e.FullPath, Project.Name));
	}

	void OnChanged (object sender, FileSystemEventArgs e)
	{
		Logger.Information (string.Format ("Changes detected for {0} in {1} project.", e.FullPath, Project.Name));
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~ProjectFileChangeMonitor()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose ()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose (disposing: true);
		GC.SuppressFinalize (this);
	}
}
