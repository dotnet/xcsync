// Copyright (c) Microsoft Corporation.  All rights reserved.

using Serilog;

namespace xcsync;

class SyncContextBase (string projectPath, string targetDir, string framework, ILogger logger)
	: ISyncContext {
	protected readonly ILogger Logger = logger;

	protected string ProjectPath { get; } = projectPath;
	protected string TargetDir { get; } = targetDir;
	protected string Framework { get; } = framework;

	/* ITypeSystem TypeSystem { get; } */
}

