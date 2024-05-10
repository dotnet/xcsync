using System.IO.Abstractions;
using xcsync.Projects;

namespace xcsync.tests.Projects;

public class NSProjectTest : Base {

	[Fact]
	public async Task GetTypes ()
	{
		var expectedTypes = new List<string> {
			"AlsoNoSkip",
			"AppDelegate",
			"ModelVariety",
			"NoSkip",
			"ViewController",
		};

		(_, NSProject xcodeProject) = InitializeProjects ();

		Assert.Equal (expectedTypes.Count, await xcodeProject.GetTypes ().CountAsync ());
		await foreach (var type in xcodeProject.GetTypes ()) {
			Assert.Contains (type.CliType, expectedTypes);
		}
	}

	[InlineData ("ModelVariety", "ObjectiveCModelVariety", true, false, "NSObject", "NSObject", false)]
	[InlineData ("ViewController", "ViewController", false, true, "NSViewController", "NSViewController", false)]
	[Theory]
	public async Task ConvertToNSObject (string cliName, string objcName, bool isModel, bool inDesigner, string cliBaseName, string objcBaseName, bool baseIsModel)
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == cliName);

		Assert.NotNull (nsType);
		Assert.Equal (objcName, nsType.ObjCType);
		Assert.Equal (isModel, nsType.IsModel);
		Assert.Equal (inDesigner, nsType.InDesigner);
		Assert.Equal (cliBaseName, nsType?.BaseType?.CliType);
		Assert.Equal (objcBaseName, nsType?.BaseType?.ObjCType);
		Assert.Equal (baseIsModel, nsType?.BaseType?.IsModel);
	}

	[Fact]
	public async Task ViewControllerOutlets ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "ViewController");

		Assert.NotNull (nsType);
		var outlets = nsType.Outlets;

		Assert.NotNull (outlets);
		Assert.Single (outlets);
		Assert.Equal ("FileLabel", outlets [0].CliName);
		Assert.Equal ("FileLabel", outlets [0].ObjCName);
		Assert.Equal ("NSTextField", outlets [0].CliType);
		Assert.Equal ("NSTextField", outlets [0].ObjCType);
	}

	[Fact]
	public async Task ViewControllerActions ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "ViewController");

		Assert.NotNull (nsType);
		var actions = nsType.Actions;

		Assert.NotNull (actions);
		Assert.Single (actions);
		Assert.Equal ("UploadButton", actions [0].CliName);
		Assert.Equal ("UploadButton", actions [0].ObjCName);
		Assert.Single (actions [0].Parameters);

		Assert.Equal ("sender", actions [0].Parameters [0].CliName);
		Assert.Null (actions [0].Parameters [0].ObjCName);

		Assert.Equal ("NSObject", actions [0].Parameters [0].CliType);
		Assert.Equal ("NSObject", actions [0].Parameters [0].ObjCType);
	}

	[Fact]
	public async Task NoOutletActionAttribute ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "AppDelegate");

		Assert.NotNull (nsType);
		Assert.Null (nsType.Outlets);
		Assert.Null (nsType.Actions);
	}

	[Fact]
	public async Task ModelOutletAndAction ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "ModelVariety");

		Assert.NotNull (nsType);
		Assert.Null (nsType.Outlets);
		Assert.Null (nsType.Actions);
	}

	(Dotnet, NSProject) InitializeProjects ()
	{
		Assert.True (File.Exists (TestProjectPath));

		var cliProject = new Dotnet (TestProjectPath, "net8.0-macos");
		// TODO: Convert this to MockFileSystem
		var xcodeProject = new NSProject (new FileSystem (), cliProject, "macos");
		return (cliProject, xcodeProject);
	}

}
