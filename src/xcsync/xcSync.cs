// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.CommandLine;
using Serilog;
using xcsync.Commands;
using InvalidOperationException = System.InvalidOperationException;

namespace xcsync;

public static class xcSync {

	public static async Task Main (string [] args)
	{
		Console.WriteLine (Strings.ProgramHeader);

		var force = new Option<bool> (
			["--force", "--f"],
			description: Strings.Options.ForceDescription,
			getDefaultValue: () => false);

		var project = new Option<string> (
			["--project", "--p"],
			description: Strings.Options.ProjectDescription,
			getDefaultValue: () => string.Empty);

		project.AddValidator (result => {
			var value = result.GetValueForOption (project);
			var path = string.IsNullOrEmpty (value) ? GetCsprojPath () : value;
			var error = ValidateCSharpProject (path);
			result.ErrorMessage = error is null ? null : Strings.Options.ProjectValidationError(error);
		});

		var target = new Option<string> (
			["--target", "--t"],
			description: Strings.Options.TargetDescription,
			getDefaultValue: () => string.Empty);

		target.AddValidator (result => {
			var value = result.GetValueForOption (target);
			var path = string.IsNullOrEmpty (value) ? DefaultXcodeProjectOutputPath : value!;
			var error = ValidateXcodeProject (path, result.GetValueForOption (force));
			result.ErrorMessage = error is null ? null : Strings.Options.TargetValidationError(error);
		});

		var open = new Option<bool> (
			["--open", "--o"],
			description: Strings.Options.OpenDescription,
			getDefaultValue: () => false);

		var tfm = new Option<string> (
			["--framework", "--tfm", "--target-framework", "--target-frameworks"],
			description: Strings.Options.TfmDescription,
			getDefaultValue: () => string.Empty);

		var verbosity = new Option<LogLevel> (
			["--verbosity", "-v"],
			description: Strings.Options.VerbosityDescription,
			getDefaultValue: () => LogLevel.Information
		);

		var generate = new Command ("generate", Strings.Commands.GenerateDescription)
		{
			project,
			target,
			force,
			open,
			tfm,
		};

		var sync = new Command ("sync", Strings.Commands.SyncDescription)
		{
			project,
			target,
			force,
		};

		var watch = new Command ("watch", Strings.Commands.WatchDescription)
		{
			project,
			target,
			force,
		};

		generate.SetHandler (GenerateCommand.Execute, project, target, force, open, verbosity, tfm);
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
			0 => throw new FileNotFoundException (Strings.Errors.CsprojNotFound),
			1 => csprojFiles.First (),
			_ => throw new InvalidOperationException (Strings.Errors.MultipleProjectFilesFound)
		};
	}

	public static string DefaultXcodeProjectOutputPath => Path.Combine (Directory.GetCurrentDirectory (), "xcode");

	public static LoggerConfiguration? LoggerFactory { get; private set; }
	public static ILogger? Logger { get; private set; }

}
