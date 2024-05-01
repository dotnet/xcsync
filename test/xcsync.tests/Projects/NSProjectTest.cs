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

		Assert.Equal (expectedTypes.Count, NsProject.GetTypes ().CountAsync ().Result);
		await foreach (var type in NsProject.GetTypes ()) {
			Assert.Contains (type.CliType, expectedTypes);
		}
	}

	[InlineData ("ModelVariety", "ObjectiveCModelVariety", true, false, "NSObject", "NSObject", false)]
	[InlineData ("ViewController", "ViewController", false, true, "NSViewController", "NSViewController", false)]
	[Theory]
	public async Task ConvertToNSObject (string cliName, string objcName, bool isModel, bool inDesigner, string cliBaseName, string objcBaseName, bool baseIsModel)
	{
		var types = await NsProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
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
		var types = await NsProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
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
		var types = await NsProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
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
		var types = await NsProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "AppDelegate");

		Assert.NotNull (nsType);
		Assert.Null (nsType.Outlets);
		Assert.Null (nsType.Actions);
	}

	[Fact]
	public async Task ModelOutletAndAction ()
	{
		var types = await NsProject.GetTypes ().ToListAsync ().ConfigureAwait (false);
		var nsType = types.Select (x => x).FirstOrDefault (x => x.CliType == "ModelVariety");

		Assert.NotNull (nsType);
		Assert.Null (nsType.Outlets);
		Assert.Null (nsType.Actions);
	}
}
