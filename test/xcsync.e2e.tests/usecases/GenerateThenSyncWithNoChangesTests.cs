// Copyright (c) Microsoft Corporation. All rights reserved.

using Xamarin;
using Xunit.Abstractions;

namespace xcsync.e2e.tests.UseCases;

public partial class GenerateThenSyncWithNoChangesTests (ITestOutputHelper testOutput) : Base (testOutput) {
	
	[Theory]
	[InlineData("macos", "net8.0-macos")] 
	[InlineData("maccatalyst", "net8.0-maccatalyst")]
	[InlineData("ios", "net8.0-ios")]
	[InlineData("tvos", "net8.0-tvos")] 
	[InlineData ("maui", "net8.0-ios")]
	[InlineData ("maui", "net8.0-maccatalyst")]
	[Trait ("Category", "IntegrationTest")]
	public async Task GenerateThenSync_WithNoChanges_GeneratesNoChangesAsync (string projectType, string tfm)
	{
		// Arrange

		var projectName = Guid.NewGuid ().ToString ();

		var tmpDir = Cache.CreateTemporaryDirectory (projectName);

		var xcodeDir = Path.Combine (tmpDir, "obj", "xcode");

		Directory.CreateDirectory (xcodeDir);

		var csproj = Path.Combine (tmpDir, $"{projectName}.csproj");

		await Git (TestOutput, "init", tmpDir).ConfigureAwait (false);
		await DotnetNew (TestOutput, "gitignore", tmpDir, string.Empty).ConfigureAwait (false);
		await DotnetNew (TestOutput, "editorconfig", tmpDir, string.Empty).ConfigureAwait (false);
		await DotnetNew (TestOutput, "nugetconfig", tmpDir, string.Empty).ConfigureAwait (false);
		await DotnetNew (TestOutput, projectType, tmpDir, string.Empty).ConfigureAwait (false);
		await DotnetFormat (TestOutput, tmpDir).ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "add", ".").ConfigureAwait (false);
		await Git (TestOutput, "-C", tmpDir, "commit", "-m", "Initial commit").ConfigureAwait (false);

		// Act

		await Xcsync (TestOutput, "generate", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false);
		await Xcsync (TestOutput, "sync", "--project", csproj, "--target", xcodeDir, "-tfm", tfm).ConfigureAwait (false);

		// Assert
		var commandOutput = new CaptureOutput(TestOutput);
		var changesPresent = await Git (commandOutput, "-C", tmpDir, "diff", "-w", "--exit-code", "--").ConfigureAwait (false);
		if (changesPresent == 1)
			Assert.Fail ($"Git diff failed, there are changes in the source files.\n{commandOutput.Output}");
	}
}
