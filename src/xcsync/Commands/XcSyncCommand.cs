// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

		Options.Add( SharedOptions.Verbose);
		Options.Add( SharedOptions.DotnetPath);

		SharedOptions.Verbose.Validators.Add (result => {
			try {
				var value = result.GetValue (SharedOptions.Verbose);
				if (!result.Implicit && result.Tokens.Count == 0) {
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
			} catch (InvalidOperationException) {
				result.AddError(Strings.Errors.Validation.InvalidVerbosity);
			}
		});

		SharedOptions.DotnetPath.Validators.Add (result => {
			xcSync.DotnetPath = result.GetValue (SharedOptions.DotnetPath) ?? string.Empty;
			Logger?.Debug (Strings.Base.DotnetPath (xcSync.DotnetPath));
		});

		Subcommands.Add (new GenerateCommand (fileSystem, Logger));
		Subcommands.Add (new SyncCommand (fileSystem, Logger));
		Subcommands.Add (new WatchCommand (fileSystem, Logger));
	}
}
