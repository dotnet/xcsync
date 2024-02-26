using Microsoft.CodeAnalysis;
using xcsync.Projects;

namespace xcsync.tests.Projects;

public class DotnetTest {

	const string TestProjectPath = "../../../../test-project/test-project.csproj";

	[Fact]
	public async Task GetTypes ()
	{
		var expectedTypes = new List<string> {
			"AlsoNoSkip",
			"AppDelegate",
			"NoRegisterButStillValid",
			"NoSkip",
			"ViewController",
		};
		var dotnet = new Dotnet (TestProjectPath);
		var project = await dotnet.OpenProject ().ConfigureAwait (false);
		List<INamedTypeSymbol> types = await dotnet.GetTypes (project).ToListAsync ().ConfigureAwait (false);
		Assert.Equal (expectedTypes, types.Select (x => x.Name).OrderBy (x => x).ToList ());
	}
}
