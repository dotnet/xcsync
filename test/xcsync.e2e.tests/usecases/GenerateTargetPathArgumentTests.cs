// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xamarin;
using Xunit.Abstractions;

namespace xcsync.e2e.tests.UseCases;

public partial class GenerateTargetPathArgumentTests :
	Base,
	IClassFixture<GenerateTargetPathArgumentTests.TargetPathTestsFixture>,
	IDisposable {
	string targetPath = string.Empty;
	readonly TargetPathTestsFixture fixture;

	[Theory]
	[InlineData ("", 0, true)]
	[InlineData ("artifacts/xcsync", 1, false)]
	[InlineData ("{IntermediateOutputPath}/xcsync", 0, true)]
	[InlineData ("{CacheRoot}/artifacts/xcsync", 1, false)]
	[Trait ("Category", "IntegrationTest")]
	public async Task TargetPath_NoForceAndFolderDoesNotExist (string targetPath, int expectedExitCode, bool expectedDirectortyExists)
	{
		await TargetPath_BaseTest (targetPath, expectedExitCode, expectedDirectortyExists,
			() => [],
			(path) => { }
		);
	}

	[Theory]
	[InlineData ("", 0, true)]
	[InlineData ("artifacts/xcsync", 0, true)]
	[InlineData ("{IntermediateOutputPath}/xcsync", 0, true)]
	[InlineData ("{CacheRoot}/artifacts/xcsync", 0, true)]
	[Trait ("Category", "IntegrationTest")]
	public async Task TargetPath_NoForceAndFolderExistsAndIsEmpty (string targetPath, int expectedExitCode, bool expectedDirectortyExists)
	{
		await TargetPath_BaseTest (targetPath, expectedExitCode, expectedDirectortyExists,
			() => [],
			(path) => {
				Directory.CreateDirectory (path);
			}
		);
	}

	[Theory]
	[InlineData ("", 1, true)]
	[InlineData ("artifacts/xcsync", 1, true)]
	[InlineData ("{IntermediateOutputPath}/xcsync", 1, true)]
	[InlineData ("{CacheRoot}/artifacts/xcsync", 1, true)]
	[Trait ("Category", "IntegrationTest")]
	public async Task TargetPath_NoForceAndFolderExistsAndIsNotEmpty (string targetPath, int expectedExitCode, bool expectedDirectortyExists)
	{
		await TargetPath_BaseTest (targetPath, expectedExitCode, expectedDirectortyExists,
			() => [],
			(path) => {
				Directory.CreateDirectory (path);
				File.WriteAllText (Path.Combine (path, "file.txt"), "content");
			}
		);
	}

	[Theory]
	[InlineData ("", 0, true)]
	[InlineData ("artifacts/xcsync", 0, true)]
	[InlineData ("{IntermediateOutputPath}/xcsync", 0, true)]
	[InlineData ("{CacheRoot}/artifacts/xcsync", 0, true)]
	[Trait ("Category", "IntegrationTest")]
	public async Task TargetPath_WithForceAndFolderDoesNotExist (string targetPath, int expectedExitCode, bool expectedDirectortyExists)
	{
		await TargetPath_BaseTest (targetPath, expectedExitCode, expectedDirectortyExists,
			() => ["--force"],
			(path) => { }
		);
	}

	[Theory]
	[InlineData ("", 0, true)]
	[InlineData ("artifacts/xcsync", 0, true)]
	[InlineData ("{IntermediateOutputPath}/xcsync", 0, true)]
	[InlineData ("{CacheRoot}/artifacts/xcsync", 0, true)]
	[Trait ("Category", "IntegrationTest")]
	public async Task TargetPath_WithForceAndFolderExistsAndIsEmpty (string targetPath, int expectedExitCode, bool expectedDirectortyExists)
	{
		await TargetPath_BaseTest (targetPath, expectedExitCode, expectedDirectortyExists,
			() => ["--force"],
			(path) => {
				Directory.CreateDirectory (path);
			}
		);
	}

	[Theory]
	[InlineData ("", 0, true)]
	[InlineData ("artifacts/xcsync", 0, true)]
	[InlineData ("{IntermediateOutputPath}/xcsync", 0, true)]
	[InlineData ("{CacheRoot}/artifacts/xcsync", 0, true)]
	[Trait ("Category", "IntegrationTest")]
	public async Task TargetPath_WithForceAndFolderExistsAndIsNotEmpty (string targetPath, int expectedExitCode, bool expectedDirectortyExists)
	{
		await TargetPath_BaseTest (targetPath, expectedExitCode, expectedDirectortyExists,
			() => ["--force"],
			(path) => {
				Directory.CreateDirectory (path);
				File.WriteAllText (Path.Combine (path, "file.txt"), "content");
			}
		);
	}

	public class TargetPathTestsFixture : IDisposable {
		public string RootPath { get; private set; } = string.Empty;
		public string IntermediateOutputPath { get; private set; } = string.Empty;
		public string [] Args { get; set; } = [];

		public void Dispose ()
		{
			// Cleanup
			GC.SuppressFinalize (this);
		}

		public async Task InitAsync (ITestOutputHelper TestOutput)
		{
			if (RootPath != string.Empty) return;

			var projectName = Guid.NewGuid ().ToString ();
			var projectType = "macos";
			var tfm = "net8.0-macos";
			RootPath = Cache.CreateTemporaryDirectory (projectName);

			var csproj = Path.Combine (RootPath, $"{projectName}.csproj");

			await DotnetNew (TestOutput, projectType, RootPath, string.Empty).ConfigureAwait (false);

			IntermediateOutputPath = Scripts.GetIntermediateOutputPath (csproj, tfm);

			Args = [
				"generate",
				"--project",
				csproj,
				"-tfm",
				tfm,
				"--verbosity",
				"diagnostic"
			];
		}
	}

	async Task TargetPath_BaseTest (string targetPath, int expectedExitCode, bool expectedDirectortyExists, Func<string []> getAdditionalCommandLineArgs, Action<string> arrangeTargetPathFolder)
	{
		// Arrange
		var cacheRoot = Cache.GetRoot ();
		var intermediateOutputPath = fixture.IntermediateOutputPath;
		var rootedTargetPath = targetPath
			.Replace ("{IntermediateOutputPath}", intermediateOutputPath)
			.Replace ("{CacheRoot}", cacheRoot);

		this.targetPath = rootedTargetPath;
		var args = fixture.Args;

		if (rootedTargetPath != string.Empty)
			args = [.. args, "--target", rootedTargetPath];

		if (rootedTargetPath == string.Empty)
			rootedTargetPath = Path.Combine (fixture.RootPath, fixture.IntermediateOutputPath, "xcsync");
		else
			rootedTargetPath = Path.Combine (fixture.RootPath, rootedTargetPath);

		args = [.. args, .. getAdditionalCommandLineArgs ()];

		arrangeTargetPathFolder (rootedTargetPath);

		// Act		
		var exitCode = await Xcsync (TestOutput, args).ConfigureAwait (false);

		// Assert
		Assert.Equal (expectedExitCode, exitCode);
		Assert.Equal (expectedDirectortyExists, Directory.Exists (rootedTargetPath));
	}

	public GenerateTargetPathArgumentTests (ITestOutputHelper testOutput, TargetPathTestsFixture fixture) : base (testOutput)
	{
		this.fixture = fixture;
		this.fixture.InitAsync (testOutput).Wait ();
	}

	public void Dispose ()
	{
		// Cleanup
		SafeDirectoryDelete (Path.Combine (fixture.RootPath, fixture.IntermediateOutputPath));
		var rootedTarget = targetPath;
		if (!Path.IsPathRooted (targetPath)) {
			var targetRoot = Path.GetDirectoryName (targetPath)?.Split (Path.DirectorySeparatorChar) [0];
			if (targetRoot != null) {
				rootedTarget = Path.Combine (fixture.RootPath, targetRoot);
			}
		}
		SafeDirectoryDelete (rootedTarget);
		GC.SuppressFinalize (this);
	}

	void SafeDirectoryDelete (string root)
	{
		if (Directory.Exists (root)) {
			TestOutput.WriteLine ($">> Deleting {root}");
			var movedRoot = root + DateTime.UtcNow.Ticks.ToString () + "-deletion-in-progress";
			// The temporary directory can be big, and it can take a while to clean it out.
			// So move it to a different name (which should be fast), and then do the deletion on a different thread.
			// This should speed up startup in some cases.
			if (!Directory.Exists (movedRoot)) {
				try {
					Directory.Move (root, movedRoot);
					ThreadPool.QueueUserWorkItem ((v) => {
						try { Directory.Delete (movedRoot, true); } catch { }
					});
				} catch {
					// Just delete the root if we can't move the temporary directory.
					Directory.Delete (root, true);
				}
			} else {
				Directory.Delete (root, true);
			}
		}
	}
}
