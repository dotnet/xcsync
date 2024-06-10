using System.IO.Abstractions;
using Moq;
using Serilog;
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
			Assert.Contains (type.ClrType, expectedTypes);
		}
	}

	[InlineData ("ModelVariety", "ObjectiveCModelVariety", true, false, "NSObject", "NSObject", false)]
	[InlineData ("ViewController", "ViewController", false, true, "NSViewController", "NSViewController", false)]
	[Theory]
	public async Task ConvertToNSObject (string clrName, string objcName, bool isModel, bool inDesigner, string clrBaseName, string objcBaseName, bool baseIsModel)
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.ClrType == clrName);

		Assert.NotNull (nsType);
		Assert.Equal (objcName, nsType.ObjCType);
		Assert.Equal (isModel, nsType.IsModel);
		Assert.Equal (inDesigner, nsType.InDesigner);
		Assert.Equal (clrBaseName, nsType.BaseType?.ClrType);
		Assert.Equal (objcBaseName, nsType.BaseType?.ObjCType);
		Assert.Equal (baseIsModel, nsType.BaseType?.IsModel);
	}

	[Fact]
	public async Task ViewControllerOutlets ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.ClrType == "ViewController");

		Assert.NotNull (nsType);
		var outlets = nsType.Outlets;

		Assert.NotNull (outlets);
		Assert.Single (outlets);
		Assert.Equal ("FileLabel", outlets [0].ClrName);
		Assert.Equal ("FileLabel", outlets [0].ObjCName);
		Assert.Equal ("NSTextField", outlets [0].ClrType);
		Assert.Equal ("NSTextField", outlets [0].ObjCType);
	}

	[Fact]
	public async Task ViewControllerActions ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.ClrType == "ViewController");

		Assert.NotNull (nsType);
		var actions = nsType.Actions;

		Assert.NotNull (actions);
		Assert.Single (actions);
		Assert.Equal ("UploadButton", actions [0].ClrName);
		Assert.Equal ("UploadButton", actions [0].ObjCName);
		Assert.Single (actions [0].Parameters);

		Assert.Equal ("sender", actions [0].Parameters [0].ClrName);
		Assert.Null (actions [0].Parameters [0].ObjCName);

		Assert.Equal ("NSObject", actions [0].Parameters [0].ClrType);
		Assert.Equal ("NSObject", actions [0].Parameters [0].ObjCType);
	}

	[Fact]
	public async Task NoOutletActionAttribute ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.ClrType == "AppDelegate");

		Assert.NotNull (nsType);
		Assert.Null (nsType.Outlets);
		Assert.Null (nsType.Actions);
	}

	[Fact]
	public async Task ModelOutletAndAction ()
	{
		(_, NSProject xcodeProject) = InitializeProjects ();

		var types = await xcodeProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.ClrType == "ModelVariety");

		Assert.NotNull (nsType);
		Assert.Null (nsType.Outlets);
		Assert.Null (nsType.Actions);
	}

	(ClrProject, NSProject) InitializeProjects ()
	{
		Assert.True (File.Exists (TestProjectPath));
		// TODO: Convert this to MockFileSystem --> needs entirety of testproject mocked
		var clrProject = new ClrProject (new FileSystem (), Mock.Of<ILogger> (), new TypeService(), "TestProject", TestProjectPath, "net8.0-macos");
		var xcodeProject = new NSProject (new FileSystem (), clrProject, "macos");
		return (clrProject, xcodeProject);
	}

}
