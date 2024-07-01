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
}
