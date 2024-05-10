using Microsoft.CodeAnalysis;
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

		var cliProject = new Dotnet (TestProjectPath, "net8.0-macos");
		var xcodeProject = new NSProject (cliProject, "macos");

		var project = await cliProject.OpenProject ().ConfigureAwait (false);
		List<INamedTypeSymbol> types = await cliProject.GetNsoTypes (project).ToListAsync ().ConfigureAwait (false);
		Assert.Equal (expectedTypes, types.Select (x => x.Name).OrderBy (x => x).ToList ());
	}
}
