// Copyright (c) Microsoft Corporation. All rights reserved.

using System.CommandLine;
using System.IO.Abstractions;
using Serilog;
using Serilog.Events;

namespace xcsync.Commands;

class XcSyncCommand : RootCommand {

	public static ILogger? Logger { get; private set; }

	public XcSyncCommand (IFileSystem fileSystem, ILogger? logger = null) : base ("xcsync")
	{
		Logger = logger ?? xcSync.Logger!
			.ForContext ("SourceContext", typeof (XcSyncCommand).Name.Replace ("Command", string.Empty).ToLowerInvariant ());

		AddGlobalOption (SharedOptions.Verbose);
		AddGlobalOption (SharedOptions.DotnetPath);

		SharedOptions.Verbose.AddValidator (result => {
			var value = result.GetValueForOption (SharedOptions.Verbose);
			if (!result.IsImplicit && result.Tokens.Count == 0) {
				value = Verbosity.Normal;
			}
			xcSync.LogLevelSwitch.MinimumLevel = value switch {
				Verbosity.Quiet => LogEventLevel.Error,
				Verbosity.Minimal => LogEventLevel.Error,
				Verbosity.Normal => LogEventLevel.Information,
				Verbosity.Detailed => LogEventLevel.Verbose,
				Verbosity.Diagnostic => LogEventLevel.Verbose,
				_ => LogEventLevel.Information,
			};
		});

		SharedOptions.DotnetPath.AddValidator (result => {
			xcSync.DotnetPath = result.GetValueForOption (SharedOptions.DotnetPath) ?? string.Empty;
			Logger?.Debug ("Using the `dotnet` located at {0}", xcSync.DotnetPath);
		});

		AddCommand (new GenerateCommand (fileSystem, Logger));
		AddCommand (new SyncCommand (fileSystem, Logger));
		AddCommand (new WatchCommand (fileSystem, Logger));
	}
}
