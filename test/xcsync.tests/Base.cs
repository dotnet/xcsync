// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.CommandLine;
using System.CommandLine.IO;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using Serilog.Events;
using Xamarin.Utils;
using Xunit.Abstractions;

namespace xcsync.tests;

public class Base : IDisposable {
	protected static readonly string DotNetExe;

	protected static readonly string XcsyncExe =
		Path.Combine (Directory.GetCurrentDirectory (), "xcsync");

	protected readonly string TestProjectPath =
		Path.Combine (SolutionPathFinder.GetProjectRoot (), "test", "test-project", "test-project.csproj");

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

	static async Task Run (ITestOutputHelper output, string path, string executable, params string [] arguments)
	{
		var outputWrapper = new LoggingOutputWriter (output);

		output.WriteLine ($"\r* {path}% {executable} {string.Join (" ", arguments)}");
		var exec = await Execution.RunAsync (
				executable,
				arguments,
				workingDirectory: path,
				standardOutput: outputWrapper,
				standardError: outputWrapper
			);
		Assert.Equal (0, exec.ExitCode);
	}

	protected static async Task DotnetNew (ITestOutputHelper output, string template, string path, string templateOptions = "") => await Run (output, path, DotNetExe, "new", template, "-o", path, templateOptions);

	public void Dispose ()
	{
		// Cleanup for all tests as DotnetPath is static
		xcSync.DotnetPath = string.Empty;
	}

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

	public class XunitLogger (ITestOutputHelper output) : ILogger {
		public void Write (LogEvent logEvent)
		{
			try {
				output.WriteLine (logEvent.RenderMessage ());
			} catch {
				// Ignore it, it's for testing only anyways, if we really need it, we can fix it then
			}
		}
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

	public class CapturingConsole : IConsole {
		readonly List<string> output = [];
		readonly List<string> error = [];

		public IStandardStreamWriter Out => new ListStreamWriter (output);

		public bool IsOutputRedirected => true;

		public IStandardStreamWriter Error => new ListStreamWriter (error);

		public bool IsErrorRedirected => true;

		public bool IsInputRedirected => false;

		public IReadOnlyList<string> Output => output.AsReadOnly ();
		public IReadOnlyList<string> ErrorOutput => error.AsReadOnly ();

		class ListStreamWriter (List<string> list) : IStandardStreamWriter {
			public void Write (string? value)
			{
				if (string.IsNullOrEmpty (value))
					return;

				foreach (var line in value.Split (Environment.NewLine)) {
					if (!string.IsNullOrEmpty (value) && !string.IsNullOrWhiteSpace (value)) {
						list.Add (line);
					}
				}
			}
		}
	}
}
