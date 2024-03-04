namespace xcsync.tests.Projects;

public class NSProjectTest : Base {

	[Fact]
	public async Task GetTypes ()
	{
		var expectedTypes = new List<string> {
			"AlsoNoSkip",
			"AppDelegate",
			"ModelVariety",
			"NoRegisterButStillValid",
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
	[InlineData ("NoRegisterButStillValid", "NoRegisterButStillValid", false, false, "ModelVariety", "ObjectiveCModelVariety", true)]
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
}
