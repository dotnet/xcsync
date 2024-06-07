using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.CodeAnalysis;
using Moq;
using Serilog;
using xcsync.Projects;

namespace xcsync.tests.Projects;

public class DotnetTest : Base {

	[Fact]
	public async Task GetTypes ()
	{
		var expectedTypes = new List<string> {
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

		var clrProject = new ClrProject (new MockFileSystem (), Mock.Of<ILogger> (), "TestProject", TestProjectPath, "net8.0-macos");
		var xcodeProject = new NSProject (new MockFileSystem (), clrProject, "macos");

		var project = await clrProject.OpenProject ().ConfigureAwait (false);
		List<INamedTypeSymbol> types = await clrProject.GetNsoTypes (project).ToListAsync ().ConfigureAwait (false);
		Assert.Equal (expectedTypes, types.Select (x => x.Name).OrderBy (x => x).ToList ());
	}
}
