// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using System.Text;
using Xamarin.Utils;
using xcsync.Projects;
using Xunit.Abstractions;

namespace xcsync.tests;

public class Base {
	protected static readonly string XcsyncExe =
		Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..", "..", "xcsync", "bin", "Debug", "net8.0", "xcsync");

	protected readonly string TestProjectPath =
		Path.Combine ("..", "..", "..", "..", "test-project", "test-project.csproj");

	protected readonly Dotnet DotnetProject;

	protected readonly NSProject NsProject;

	protected Base ()
	{
		if (!File.Exists (TestProjectPath))
			throw new FileNotFoundException ($"Test project not found at '{TestProjectPath}'");

		DotnetProject = new Dotnet (TestProjectPath, "net8.0-macos");
		NsProject = new NSProject (DotnetProject, "macos");
	}

	static void Run (ITestOutputHelper output, string path, string executable, params string [] arguments)
	{
		output.WriteLine ($"Running: {path}/{executable} {arguments}");
		var outputWrapper = new LoggingOutputWriter (output);
		var exec = Execution.RunAsync (
				executable,
				arguments,
				workingDirectory: path,
				standardOutput: outputWrapper,
				standardError: outputWrapper
			).Result;
		Assert.Equal (0, exec.ExitCode);
	}

	protected static void DotnetNew (ITestOutputHelper output, string template, string path, string templateOptions = "")
	{
		Run (output, path, "dotnet", "new", template, "-o", path, templateOptions);
	}

	protected static void Xcsync (ITestOutputHelper output, params string [] arguments)
	{
		Run (output, Directory.GetCurrentDirectory (), XcsyncExe, arguments);
	}

	class LoggingOutputWriter (ITestOutputHelper helper) : TextWriter {

		public override void WriteLine (string? value)
		{
			if (value is not null)
				helper.WriteLine (value?.ToString ());
		}

		public override Encoding Encoding {
			get {
				return Encoding.UTF8;
			}
		}
	}

}
