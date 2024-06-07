// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Serilog;
using xcsync.Projects;

namespace xcsync;

class SyncContextBase (IFileSystem fileSystem, ITypeService typeService, string projectPath, string targetDir, string framework, ILogger logger)
	: ISyncContext {
	protected readonly ILogger Logger = logger;
	protected readonly IFileSystem FileSystem = fileSystem;

	public ITypeService TypeService => typeService;

	protected string ProjectPath { get; } = projectPath;
	protected string TargetDir { get; } = targetDir;
	protected string Framework { get; } = framework;

}

