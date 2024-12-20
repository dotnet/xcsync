// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;

using Serilog;

namespace xcsync.Projects;

class SyncableProject : ISyncableProject {
	public string Name { get; init; }

	public string RootPath { get; init; }

	public ExtensionFilter ProjectFilesFilter { get; init; }

	protected IFileSystem FileSystem { get; init; }

	protected ILogger Logger { get; init; }

	protected ITypeService TypeService { get; init; }

	protected string Framework { get; set; }

	Task? initTask;

	public SyncableProject (IFileSystem fileSystem, ILogger logger, ITypeService typeService, string name, string rootPath, string framework, ExtensionFilter projectFilesFilter)
	{
		FileSystem = fileSystem;
		Logger = logger;
		TypeService = typeService;
		Name = name;
		RootPath = rootPath;
		Framework = framework;
		ProjectFilesFilter = projectFilesFilter;

		_ = InitAsync ();
	}

	public Task InitAsync ()
	{
		if (initTask == null || initTask.IsFaulted)
			initTask = InitTask ();

		return initTask;
	}

	Task InitTask () => Task.Run (InitializeAsync);

	protected virtual void InitializeAsync ()
	{
		// Override this method to perform initialization
	}
}
