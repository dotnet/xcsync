// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;

using Serilog;

namespace xcsync.Projects;

class SyncableProject (IFileSystem fileSystem, ILogger logger, string name, string rootPath, string framework, string [] projectFilesFilter) : ISyncableProject {
	public string Name { get; set; } = name;

	public string RootPath { get; set; } = rootPath;

	public string [] ProjectFilesFilter { get; init; } = projectFilesFilter;

	protected IFileSystem FileSystem { get; init; } = fileSystem;

	protected ILogger Logger { get; init; } = logger;

	protected string Framework { get; set; } = framework;
}
