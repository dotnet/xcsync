using System.IO.Abstractions.TestingHelpers;
using Microsoft.CodeAnalysis;
using Moq;
using Serilog;
using xcsync.Projects;
using Xunit.Abstractions;

namespace xcsync.tests.Projects;

public class DotnetTest (ITestOutputHelper TestOutput) : Base {

	[Fact]
	public async Task GetTypes ()
	{
		var expectedTypes = new List<string?> {
			"AlsoNoSkip",
			"AppDelegate",
			"ModelVariety",
			"NoSkip",
			"ProtocolModelVariety",
			"ProtocolVariety",
			"ViewController",
		};
		if (!File.Exists (TestProjectPath))
			throw new FileNotFoundException ($"Test project not found at '{TestProjectPath}'");

		var logger = Mock.Of<ILogger> ();
		var typeService = new TypeService (logger);
		var clrProject = new ClrProject (new MockFileSystem (), logger, typeService, "TestProject", TestProjectPath, "net8.0-macos");

		var project = await clrProject.OpenProject ().ConfigureAwait (false);
		var types = typeService.QueryTypes ().ToList ();
		Assert.Equal (expectedTypes, [.. types.Where (x => x?.ClrType is not null).Select (x => x?.ClrType).OrderBy (x => x)]);
	}

	[InlineData("ios", "Program.cs", "net8.0-ios")]
	[InlineData("maui", "MauiProgram.cs", "net8.0-ios")]
	[Theory]
	public async Task GetCompilation_WithSyntaxError(string template, string fileToChange, string tfm)
	{
		var projectPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(projectPath);
		await DotnetNew(TestOutput, template, projectPath, "");

		var filePath = Path.Combine(projectPath, fileToChange);
		// modify program to have a syntax error
		await File.WriteAllTextAsync(filePath,
			"class Program { static void Main ()  Console.WriteLine (\"Hello, World!\") } }");

		var logger = Mock.Of<ILogger>();
		var typeService = new TypeService(logger);
		var csprojFile = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault()!;
		var clrProject = new ClrProject(new MockFileSystem(), logger, typeService, "TestProject", csprojFile, tfm);
		await clrProject.OpenProject().ConfigureAwait(false);

		Assert.Empty(typeService.compilations);
	}
}
