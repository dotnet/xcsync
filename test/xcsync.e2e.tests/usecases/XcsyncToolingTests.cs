// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Xamarin;
using Xamarin.Utils;
using Xunit.Abstractions;

namespace xcsync.e2e.tests.UseCases;

public class XcsyncToolingTests (ITestOutputHelper testOutput) : Base (testOutput) {

	[Fact]
	public async Task Watch_WhenInputAndOutputRedirected_DoesNotThrow ()
	{
		// Arrange
		var projectName = Guid.NewGuid ().ToString ();

		var projectType = "macos";
		var tfm = $"net8.0-{projectType}";

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcsync");

		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty).ConfigureAwait (false);

		//var exitCode = await Xcsync (TestOutput, "watch", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false);

		var arguments = new List<string> { "watch", "--project", csproj, "--target", xcodeDir, "-tfm", tfm };

		var outputWrapper = new LoggingOutputWriter (TestOutput);

		// Act
		var cts = new CancellationTokenSource ();

		var timer = new Timer (async (state) => {
			var procs = Process.GetProcessesByName ("xcsync");
			if (procs.Length == 0)
				Assert.True (false, "xcsync should be running.");
			if (procs.Length > 1)
				Assert.True (false, "more than one xcsync running");
			var xcsyncProc = procs [0];

			var kill = await Xamarin.Utils.Execution.RunAsync (
				"kill",
				new string [] { "-INT", $"{xcsyncProc.Id}" },
				workingDirectory: tmpDir,
				standardOutput: outputWrapper,
				standardError: outputWrapper,
				log: outputWrapper,
				timeout: TimeSpan.FromSeconds (30),
				cancellationToken: cts.Token);

		}, null, 15000, Timeout.Infinite);

		var exec = await Xamarin.Utils.Execution.RunAsync (
			XcsyncExe,
			arguments,
			workingDirectory: tmpDir,
			standardOutput: outputWrapper,
			standardError: outputWrapper,
			log: outputWrapper,
			timeout: TimeSpan.FromSeconds (30),
			cancellationToken: cts.Token
		).ConfigureAwait (false);

		// Assert
		Assert.Equal (130, exec.ExitCode); // 130 is the exit code when exiting using CTRL-C
	}
}
