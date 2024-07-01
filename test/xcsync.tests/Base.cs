// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text;
using Xamarin.Utils;
using Xunit.Abstractions;

namespace xcsync.tests;

public class Base {
	protected static readonly string XcsyncExe =
		Path.Combine (Directory.GetCurrentDirectory (), "xcsync");

	protected readonly string TestProjectPath =
		Path.Combine ("..", "..", "..", "..", "test-project", "test-project.csproj");

	static async Task Run (ITestOutputHelper output, string path, string executable, params string [] arguments)
	{
		output.WriteLine ($"\rRunning: {path}/{executable} {arguments}");
		var outputWrapper = new LoggingOutputWriter (output);
		var exec = await Execution.RunAsync (
				executable,
				arguments,
				workingDirectory: path,
				standardOutput: outputWrapper,
				standardError: outputWrapper
			);
		Assert.Equal (0, exec.ExitCode);
	}

	protected static async Task DotnetNew (ITestOutputHelper output, string template, string path, string templateOptions = "") => await Run (output, path, "dotnet", "new", template, "-o", path, templateOptions);

	protected static async Task Xcsync (ITestOutputHelper output, params string [] arguments) => await Run (output, Directory.GetCurrentDirectory (), XcsyncExe, arguments);

	class LoggingOutputWriter (ITestOutputHelper helper) : TextWriter {

		public override void WriteLine (string? value)
		{
			if (value is not null)
				helper.WriteLine ($"\r{value?.ToString ()}");
		}

		public override Encoding Encoding {
			get {
				return Encoding.UTF8;
			}
		}
	}
}
