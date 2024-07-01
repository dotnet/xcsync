// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text;
using Serilog;
using Xamarin.Utils;
using Xunit.Abstractions;

namespace xcsync.e2e.tests;

public class Base(ITestOutputHelper testOutput)
{
	protected readonly ITestOutputHelper TestOutput = testOutput;

	protected readonly ILogger TestLogger = new LoggerConfiguration()
		.MinimumLevel.Verbose()
		.WriteTo.TestOutput(testOutput)
		.CreateLogger();

	protected static readonly string XcsyncExe = Path.Combine(Directory.GetCurrentDirectory(), "xcsync");
	protected static readonly string GitExe = "git";

	protected static async Task<int> DotnetNew(ITestOutputHelper output, string template, string path, string templateOptions = "") => await Run(output, path, "dotnet", "new", template, "-o", path, templateOptions);

	protected static async Task<int> Xcsync (ITestOutputHelper output, params string[] arguments) => await Run(output, Directory.GetCurrentDirectory(), XcsyncExe, arguments);

	protected static async Task<int> Git (ITestOutputHelper output, params string[] arguments) => await Run(output, Directory.GetCurrentDirectory(), GitExe, arguments);


	static async Task<int> Run(ITestOutputHelper output, string path, string executable, params string[] arguments)
	{
		output.WriteLine($"\rRunning: {path}/{executable} {string.Join(" ", arguments)}");
		var outputWrapper = new LoggingOutputWriter(output);
		var exec = await Execution.RunAsync(
				executable,
				arguments,
				workingDirectory: path,
				standardOutput: outputWrapper,
				standardError: outputWrapper
			).ConfigureAwait (false);
		// if (exec.ExitCode != 0)
		// 	throw new Exception($"{executable} {string.Join(" ", arguments)} failed with exit code {exec.ExitCode}");
		return exec.ExitCode;
	}

	class LoggingOutputWriter(ITestOutputHelper helper) : TextWriter
	{
		public override void WriteLine(string? value)
		{
			if (value is not null)
				helper.WriteLine($"\r{value?.ToString()}");
		}

		public override Encoding Encoding => Encoding.UTF8;
	}
}