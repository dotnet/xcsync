// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Marille;
using NuGet.Frameworks;
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
	protected NuGetFramework Framework { get; } = NuGetFramework.Parse (framework);

	protected Hub Hub = new ();
	// hub needs to be created, registered to a specific topic, published
	protected TopicConfiguration configuration = new ();
	readonly CancellationTokenSource cancellationTokenSource = new ();

	protected virtual Task ConfigureMarilleHub ()
	{
		configuration.Mode = ChannelDeliveryMode.AtMostOnceSync;
		return Task.CompletedTask;
	}
}

