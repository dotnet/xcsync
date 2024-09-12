// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Moq;
using Serilog;

namespace xcsync.tests;

public class ProjectFileChangeMonitorTests {

	private IFileSystemWatcher watcher;
	private ILogger logger;
	private ISyncableProject project;
	private IFileSystem fileSystem;
	private ProjectFileChangeMonitor monitor;

	public ProjectFileChangeMonitorTests ()
	{
		watcher = Mock.Of<IFileSystemWatcher>();
		logger = Mock.Of<ILogger>();
		project = Mock.Of<ISyncableProject>();
		fileSystem = Mock.Of<IFileSystem>();
		Mock.Get(fileSystem).Setup(fs => fs.Path.GetDirectoryName(project.RootPath)).Returns("/repos/repo/project");

		monitor = new ProjectFileChangeMonitor(fileSystem, watcher, logger);
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
	[InlineData (new string [] { }, @"/repos/repo/project/src", "Some.File")]
	[InlineData (new string [] { "*.resx", "*.cs" }, @"/repos/repo/project/src", "Some.File.cs")]
	[InlineData (new string [] { "*/Resources/*.resx", "*.cs" }, @"/repos/repo/project/src/Resources", "Some.File.resx")]
	public void OnFileChanged_ShouldBeCalled_WhenFileChangesDetected (string [] fileFilter, string filePath, string fileName)
	{
		var project = Mock.Of<ISyncableProject> (p => p.ProjectFilesFilter == fileFilter);

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
}
