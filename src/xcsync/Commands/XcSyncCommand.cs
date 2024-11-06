// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;

namespace xcsync.Commands;

class XcSyncCommand : RootCommand {

	public ILogger Logger { get; private set; }

	public XcSyncCommand (IFileSystem fileSystem, ILogger logger) : base ("xcsync")
	{
		Logger = logger;
		AddCommand (new GenerateCommand (fileSystem, Logger));
		AddCommand (new SyncCommand (fileSystem, Logger));
		if (!string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("EnableXcsyncWatch")))
			AddCommand (new WatchCommand (fileSystem, Logger));
	}
}
