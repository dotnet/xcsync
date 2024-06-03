// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;

using Serilog;

namespace xcsync.Projects;

class ClrProject (IFileSystem fileSystem, ILogger logger, string name, string projectPath, string framework)
	: SyncableProject (fileSystem, logger, name, projectPath, framework, ["*.cs", "*.csproj", "*.sln"]) {
}
