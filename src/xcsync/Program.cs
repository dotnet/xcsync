using System.CommandLine;
using xcsync.Commands;

namespace xcsync;

public static class Program {

	public static void Main (string [] args)
	{
		var force = new Option<bool> (
			new [] { "--force", "--f" },
			description: "Force the generation",
			getDefaultValue: () => false);

		var project = new Option<DirectoryInfo> (
			new [] { "--project", "--p" },
			description: "Path to the project",
			getDefaultValue: () => new DirectoryInfo (Directory.GetCurrentDirectory ()));

		project.AddValidator (result => {
			result.ErrorMessage = ValidateCSharpProject (result.GetValueForOption (project)!);
		});

		var target = new Option<DirectoryInfo> (
			new [] { "--target", "--t" },
			description: "Path to the target",
			getDefaultValue: () => new DirectoryInfo (Path.Combine (Directory.GetCurrentDirectory (), "obj", "xcode")));

		target.AddValidator (result => {
			result.ErrorMessage = ValidateXcodeProject (result.GetValueForOption (target)!, result.GetValueForOption (force));
		});

		var open = new Option<bool> (
			new [] { "--open", "--o" },
			description: "Open the generated project",
			getDefaultValue: () => false);

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

		generate.SetHandler (GenerateCommand.Execute, project, target, force, open);
		sync.SetHandler (SyncCommand.Execute, project, target, force);
		watch.SetHandler (WatchCommand.Execute, project, target, force);

		var root = new RootCommand ("xcsync")
		{
			generate,
			sync,
			watch
		};

		root.Invoke (args);
	}

	private static readonly List<OptionValidation> XcodeValidations = new () {
		OptionValidations.PathExists,
		OptionValidations.PathNameValid,
		OptionValidations.PathIsEmpty,
	};

	private static readonly List<OptionValidation> XcodeForceValidations = new () {
		OptionValidations.PathExists,
		OptionValidations.PathNameValid,
		OptionValidations.PathCleaned,
		OptionValidations.PathIsEmpty,
	};

	private static readonly List<OptionValidation> CSharpValidations = new () {
		OptionValidations.PathExists,
		OptionValidations.PathContainsValidTfm,
		OptionValidations.PathNameValid,
	};

	public static string? ValidateXcodeProject (DirectoryInfo path, bool force)
	{
		List<OptionValidation> validations = force ? XcodeForceValidations : XcodeValidations;
		return validations.Select (validation => validation (path)).FirstOrDefault (error => error is not null);
	}

	public static string? ValidateCSharpProject (DirectoryInfo path) =>
		CSharpValidations.Select (validation => validation (path)).FirstOrDefault (error => error is not null);
}
