// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using Serilog.Events;
using xcsync;
using xcsync.Commands;
using InvalidOperationException = System.InvalidOperationException;

namespace xcsync;

public static class xcSync {

	public static async Task Main (string [] args)
	{
		Console.WriteLine ($"xcSync v{typeof (xcSync).Assembly.GetName ().Version}, (c) Microsoft Corporation. All rights reserved.\n");

		var force = new Option<bool> (
			["--force", "--f"],
			description: "Force the generation",
			getDefaultValue: () => false);

		var project = new Option<string> (
			["--project", "--p"],
			description: "Path to the project",
			getDefaultValue: () => string.Empty);

		project.AddValidator (result => {
			var value = result.GetValueForOption (project);
			var path = string.IsNullOrEmpty (value) ? GetCsprojPath () : value!;
			var error = ValidateCSharpProject (path);
			result.ErrorMessage = error is null ? null : $"Invalid option 'project' provided: {error}";
		});

		var target = new Option<string> (
			["--target", "--t"],
			description: "Path to the target",
			getDefaultValue: () => string.Empty);

		target.AddValidator (result => {
			var value = result.GetValueForOption (target);
			var path = string.IsNullOrEmpty (value) ? GetXcodePath () : value!;
			var error = ValidateXcodeProject (path, result.GetValueForOption (force));
			result.ErrorMessage = error is null ? null : $"Invalid option 'target' provided: {error}";
		});

		var open = new Option<bool> (
			["--open", "--o"],
			description: "Open the generated project",
			getDefaultValue: () => false);

		var verbosity = new Option<LogLevel> (
			["--verbosity", "-v"],
			description: "Set the verbosity level",
			getDefaultValue: () => LogLevel.Information
		);

		var generate = new Command ("generate",
			"generate a Xcode project at the path specified by --target from the project identified by --project")
		{
			project,
			target,
			force,
			open,
		};

		var sync = new Command ("sync",
			"used after the generate command to synchronize changes from the generated Xcode project back to the .NET project")
		{
			project,
			target,
			force,
		};

		var watch = new Command ("watch", "combination of both generate and sync")
		{
			project,
			target,
			force,
		};

		generate.SetHandler (GenerateCommand.Execute, project, target, force, open, verbosity);
		sync.SetHandler (SyncCommand.Execute, project, target, force, verbosity);
		watch.SetHandler (WatchCommand.Execute, project, target, force, verbosity);

		var root = new RootCommand ("xcsync")
		{
			generate,
			sync,
			watch
		};
		root.AddGlobalOption (verbosity);
		await root.InvokeAsync (args);
	}

	static readonly List<OptionValidation> XcodeValidations = new () {
		OptionValidations.PathExists,
		OptionValidations.PathNameValid,
		OptionValidations.PathIsEmpty,
	};

	static readonly List<OptionValidation> XcodeForceValidations = new () {
		OptionValidations.PathExists,
		OptionValidations.PathNameValid,
		OptionValidations.PathCleaned,
		OptionValidations.PathIsEmpty,
	};

	static readonly List<OptionValidation> CSharpValidations = new () {
		OptionValidations.PathExists,
		OptionValidations.PathContainsValidTfm,
		OptionValidations.PathNameValid,
	};

	static string? ValidateXcodeProject (string path, bool force)
	{
		List<OptionValidation> validations = force ? XcodeForceValidations : XcodeValidations;
		return validations.Select (validation => validation (path)).FirstOrDefault (error => error is not null);
	}

	static string? ValidateCSharpProject (string path) =>
		CSharpValidations.Select (validation => validation (path)).FirstOrDefault (error => error is not null);

	static string GetCsprojPath ()
	{
		var csprojFiles = Directory.GetFiles (Directory.GetCurrentDirectory (), "*.csproj");
		return csprojFiles.Length switch {
			0 => throw new FileNotFoundException ("Could not find a .csproj file"),
			1 => csprojFiles.First (),
			_ => throw new InvalidOperationException (
				"Multiple .csproj files found in current directory. Specify which project to use.")
		};
	}

	static string GetXcodePath () =>
		Path.Combine (Directory.GetCurrentDirectory (), "obj", "xcode");

	public static LoggerConfiguration? LoggerFactory { get; private set; }
	public static ILogger? Logger { get; private set; }

}
