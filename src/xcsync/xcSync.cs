// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.CodeAnalysis;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using xcsync.Commands;
using xcsync.Projects;

namespace xcsync;

static class xcSync {

	public static ApplePlatforms ApplePlatforms { get; } = new ();

	static internal readonly LoggingLevelSwitch LogLevelSwitch = new (LogEventLevel.Information);
	public static ILogger? Logger { get; private set; }
	public static IFileSystem FileSystem { get; } = new FileSystem ();

	public static async Task Main (string [] args)
	{
		ConfigureLogging ();
		WriteHeader ();

		var parser = new CommandLineBuilder (new XcSyncCommand (FileSystem))
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

	static public bool TryGetTargetPlatform (ILogger logger, string tfm, [NotNullWhen (true)] out string targetPlatform)
	{
		targetPlatform = string.Empty;

		foreach (var platform in xcSync.ApplePlatforms) {
			if (tfm.Contains (platform.Key)) {
				targetPlatform = platform.Key;
				return true;
			}
		}

		logger?.Fatal (Strings.Errors.TargetPlatformNotFound);
		return false;
	}

	public static bool IsNsoDerived (TypeMapping mapping)
	{
		return IsNsoDerived (mapping.TypeSymbol);
	}

	public static bool IsNsoDerived (INamedTypeSymbol? type)
	{
		var registerAttribute = type?.GetAttributes ().FirstOrDefault (a => a.AttributeClass?.Name == "RegisterAttribute");
		var skipAttribute = registerAttribute?.NamedArguments.FirstOrDefault (x => x.Key == "SkipRegistration");

		if (skipAttribute?.Value.Value is true)
			return false;

		return registerAttribute is not null;
	}

}
