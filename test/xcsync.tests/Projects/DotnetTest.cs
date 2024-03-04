using Microsoft.CodeAnalysis;

namespace xcsync.tests.Projects;

public class DotnetTest : Base {

	[Fact]
	public async Task GetTypes ()
	{
		var expectedTypes = new List<string> {
			"AlsoNoSkip",
			"AppDelegate",
			"ModelVariety",
			"NoRegisterButStillValid",
			"NoSkip",
			"ProtocolModelVariety",
			"ProtocolVariety",
			"ViewController",
		};

		var project = await DotnetProject.OpenProject ().ConfigureAwait (false);
		List<INamedTypeSymbol> types = await DotnetProject.GetNsoTypes (project).ToListAsync ().ConfigureAwait (false);
		Assert.Equal (expectedTypes, types.Select (x => x.Name).OrderBy (x => x).ToList ());
	}
}
