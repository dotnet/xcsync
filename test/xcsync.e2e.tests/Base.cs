// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using Xamarin.Utils;
using Xunit.Abstractions;

namespace xcsync.e2e.tests;

public class Base (ITestOutputHelper testOutput) {
	protected static readonly string DotNetExe;

	protected readonly ITestOutputHelper TestOutput = testOutput;

	protected readonly ILogger TestLogger = new LoggerConfiguration ()
		.MinimumLevel.Verbose ()
		.WriteTo.TestOutput (testOutput)
		.CreateLogger ();

	protected static readonly string XcsyncExe = Path.Combine (Directory.GetCurrentDirectory (), "xcsync");
	protected static readonly string GitExe = "git";
	protected static readonly string PatchExe = "patch";

	static Base ()
	{
		var output = new OutputCapture ();
		var command = RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ? "where" : "which";
		var exec = Execution.RunAsync (command, arguments: ["dotnet"], standardOutput: output).Result;
		if (exec.ExitCode != 0)
			throw new Exception ("Failed to find path to dotnet.");
		// Set the path to the dotnet executable
		DotNetExe = output.ToString ().Trim ();
	}

	class OutputCapture : TextWriter {
		readonly StringBuilder output = new ();

		public override void WriteLine (string? value)
		{
			if (value != null) {
				output.AppendLine (value);
			}
		}

		public override Encoding Encoding {
			get {
				return Encoding.UTF8;
			}
		}

		public override string ToString ()
		{
			return output.ToString ();
		}
	}

	protected static async Task<int> Dotnet (ITestOutputHelper output, string path, string command = "") => await Run (output, path, DotNetExe, command);

	protected static async Task<int> DotnetNew (ITestOutputHelper output, string template, string path, string templateOptions = "") => await Run (output, path, DotNetExe, "new", template, "-o", path, templateOptions);

	protected static async Task<int> DotnetFormat (ITestOutputHelper output, string path) => await Run (output, path, DotNetExe, "format", "--include-generated", "--no-restore");

	protected static async Task<int> Xcsync (ITestOutputHelper output, params string [] arguments) => await Run (output, Directory.GetCurrentDirectory (), XcsyncExe, arguments);

	protected static async Task<int> Git (ITestOutputHelper output, params string [] arguments) => await Run (output, Directory.GetCurrentDirectory (), GitExe, arguments);

	protected static async Task<int> Patch (ITestOutputHelper output, string path, string diff) => await Run (output, path, PatchExe, ["-i", diff]);

	static async Task<int> Run (ITestOutputHelper output, string path, string executable, params string [] arguments)
	{
		var outputWrapper = new LoggingOutputWriter (output);
		output.WriteLine ($"\r* {path}% {executable} {string.Join (" ", arguments)}");
		var exec = await Execution.RunAsync (
			executable,
			arguments,
			workingDirectory: path,
			standardOutput: outputWrapper,
			standardError: outputWrapper,
			log: outputWrapper,
			timeout: TimeSpan.FromSeconds (30)
		).ConfigureAwait (false);
		return exec.ExitCode;
	}

	class LoggingOutputWriter (ITestOutputHelper helper) : TextWriter {
		public override void WriteLine (string? value)
		{
			if (value is not null)
				helper.WriteLine ($"\r{value?.ToString ()}");
		}

		public override Encoding Encoding => Encoding.UTF8;
	}

	protected class CaptureOutput (ITestOutputHelper helper) : ITestOutputHelper {
		StringBuilder outputBuilder = new ();
		public string Output { get { return outputBuilder.ToString (); } }

		public void WriteLine (string message)
		{
			outputBuilder.Append (message);
			helper.WriteLine (message);
		}

		public void WriteLine (string format, params object [] args)
		{
			outputBuilder.Append (string.Format (format, args));
			outputBuilder.Append (Environment.NewLine);
			helper.WriteLine (format, args);
		}
	}

}
