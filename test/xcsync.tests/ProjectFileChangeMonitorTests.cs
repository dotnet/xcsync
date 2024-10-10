// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Moq;
using Serilog;

namespace xcsync.tests;

public class ProjectFileChangeMonitorTests {

	IFileSystemWatcher watcher;
	ILogger logger;
	ISyncableProject project;
	IFileSystem fileSystem;
	ProjectFileChangeMonitor monitor;

	public ProjectFileChangeMonitorTests ()
	{
		watcher = Mock.Of<IFileSystemWatcher> ();
		logger = Mock.Of<ILogger> ();
		project = Mock.Of<ISyncableProject> ();
		Mock.Get (project).Setup (p=> p.ProjectFilesFilter).Returns (new ExtensionFilter ([".cs", ".resx", ".File"]));
		fileSystem = Mock.Of<IFileSystem> ();
		Mock.Get (fileSystem).Setup (fs => fs.Path.GetDirectoryName (project.RootPath)).Returns ("/repos/repo/project");

		monitor = new ProjectFileChangeMonitor (fileSystem, watcher, logger);
	}

	[Fact]
	public void StartMonitoring_ShouldEnableRaisingEvents ()
	{
		// Act
		monitor.StartMonitoring (project);

		// Assert
		Assert.True (watcher.EnableRaisingEvents);
	}

	[Fact]
	public void StopMonitoring_ShouldDisableRaisingEvents ()
	{
		Assert.False (watcher.EnableRaisingEvents);
		monitor.StartMonitoring (project);
		Assert.True (watcher.EnableRaisingEvents);

		// Act
		monitor.StopMonitoring ();

		// Assert
		Assert.False (watcher.EnableRaisingEvents);
	}

	[Theory]
	[InlineData (new string [] {".File" }, @"/repos/repo/project/src", "Some.File")]
	[InlineData (new string [] { ".resx", ".cs" }, @"/repos/repo/project/src", "Some.File.cs")]
	[InlineData (new string [] { ".resx", ".cs" }, @"/repos/repo/project/src/Resources", "Some.File.resx")]
	public void OnFileChanged_ShouldBeCalled_WhenFileChangesDetected (string [] fileFilter, string filePath, string fileName)
	{
		var project = Mock.Of<ISyncableProject> (p => p.ProjectFilesFilter == new ExtensionFilter (fileFilter));

		var fileChanged = false;
		monitor.OnFileChanged =
			path => fileChanged = true;

		// Act
		monitor.StartMonitoring (project);

		Mock.Get (watcher).Raise (w => w.Created += null, new FileSystemEventArgs (WatcherChangeTypes.Created, filePath, fileName));

		// Assert
		Assert.True (fileChanged);
	}

	[Fact]
	public void OnFileRenamed_ShouldBeCalled_WhenFileIsRenamed ()
	{
		var fileRenamed = false;
		monitor.OnFileRenamed = (oldPath, newPath) => fileRenamed = true;

		// Act
		monitor.StartMonitoring (project);
		Mock.Get (watcher).Raise (w => w.Renamed += null, new RenamedEventArgs (WatcherChangeTypes.Renamed, @"/Some/Directory", "Some.File", "Some.File.Renamed"));

		// Assert
		Assert.True (fileRenamed);
	}

	[Fact]
	public void OnError_ShouldBeCalled_WhenErrorOccurs ()
	{
		var errorOccurred = false;
		monitor.OnError = ex => errorOccurred = true;

		// Act
		monitor.StartMonitoring (project);
		Mock.Get (watcher).Raise (w => w.Error += null, new ErrorEventArgs (new Exception ("An error occurred.")));

		// Assert
		Assert.True (errorOccurred);
	}



	[InlineData (WatcherChangeTypes.Changed)]
	[InlineData (WatcherChangeTypes.Created)]
	[InlineData (WatcherChangeTypes.Deleted)]
	[InlineData (WatcherChangeTypes.Renamed)]
	[Theory]
	public void StarMonitoring_ShouldCaptureEvents_WithNoException (WatcherChangeTypes eventType)
	{
		var fileChanged = false;
		monitor.OnFileChanged = path => fileChanged = true;
		monitor.OnFileRenamed = (oldPath, newPath) => fileChanged = true;

		// Act
		monitor.StartMonitoring (project);

		switch (eventType) {
		case WatcherChangeTypes.Changed:
			Mock.Get (watcher).Raise (w => w.Changed += null, new FileSystemEventArgs (eventType, @"/repos/repo/project/src", "Some.File"));
			break;
		case WatcherChangeTypes.Created:
			Mock.Get (watcher).Raise (w => w.Created += null, new FileSystemEventArgs (eventType, @"/repos/repo/project/src", "Some.File"));
			break;
		case WatcherChangeTypes.Deleted:
			Mock.Get (watcher).Raise (w => w.Deleted += null, new FileSystemEventArgs (eventType, @"/repos/repo/project/src", "Some.File"));
			break;
		case WatcherChangeTypes.Renamed:
			Mock.Get (watcher).Raise (w => w.Renamed += null, new RenamedEventArgs (eventType, @"/repos/repo/project/src", "Some.File", "Old.File"));
			break;
		}

		// Assert
		Assert.True (fileChanged);
	}
}
