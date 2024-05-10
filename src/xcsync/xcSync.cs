// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using xcsync.Commands;

namespace xcsync;

static class xcSync {

#pragma warning disable CA2211 // Non-constant fields should not be visible
	public static ApplePlatforms ApplePlatforms = new ();

#pragma warning restore CA2211 // Non-constant fields should not be visible

	static internal readonly LoggingLevelSwitch LogLevelSwitch = new (LogEventLevel.Information);
	public static ILogger? Logger { get; private set; }

	public static async Task Main (string [] args)
	{
		ConfigureLogging ();
		WriteHeader ();

		var parser = new CommandLineBuilder (new XcSyncCommand ())
			.UseDefaults ()
			.Build ();

		await parser.InvokeAsync (args).ConfigureAwait (false);
	}

	static void ConfigureLogging ()
	{
		Logger = new LoggerConfiguration ()
					.MinimumLevel.ControlledBy (LogLevelSwitch)
					.Enrich.WithThreadName ()
					.Enrich.WithThreadId ()
					.Enrich.FromLogContext ()
					.WriteTo.Console (new ExpressionTemplate ("{#if SourceContext is not null}[{@t:HH:mm:ss} {@l:u3} {SourceContext} ({ThreadId})] {#end}{@m}\n"))
					.CreateLogger ();
	}

	static void WriteHeader () => WriteLine ($"xcSync v{typeof (xcSync).Assembly.GetName ().Version}, (c) Microsoft Corporation. All rights reserved.\n");
	static void WriteLine (string messageTemplate, params object? []? properyValues) => Logger?.Information (messageTemplate, properyValues);
}
