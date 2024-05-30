using xcsync.Projects;

namespace xcsync.tests.Projects;

public class TypeServiceTest {
	readonly TypeService typeService;

	public TypeServiceTest ()
	{
		typeService = new TypeService ();
		typeService.AddType ("ViewController", "ViewController", null, false, false, false, null, null, new HashSet<string> ());
		typeService.AddType ("AppDelegate", "AppDelegate", null, false, false, false, null, null, new HashSet<string> ());
		typeService.AddType ("TypeExtension", "TypeExtension", null, false, false, false, null, null, new HashSet<string> ());
	}

	[Fact]
	public void AddType ()
	{
		var type = new TypeMapping ("NewType", "NewType", null, false, false, false, null, null, new HashSet<string> ());
		var result = typeService.AddType (type);
		Assert.NotNull (result);
		Assert.Equal (type, result);
	}

	[Fact]
	public void QueryExistentTypes ()
	{
		var type = new TypeMapping ("NewTypeCli", "NewTypeObjC", null, false, false, false, null, null, new HashSet<string> ());
		typeService.AddType (type);
		var result = typeService.QueryTypes (objcType: "NewTypeObjC");
		Assert.Single (result);
		Assert.Equal (type, result.First ());
	}

	[Fact]
	public void QueryNonexistentType ()
	{
		var result = typeService.QueryTypes (cliType: "doesNotExist");
		Assert.Empty (result);
	}

	[Fact]
	public void QueryMultipleTypes ()
	{
		var result = typeService.QueryTypes ();
		Assert.Equal (3, result.Count ());
	}

	[Fact]
	public void AddDuplicateType ()
	{
		var viewControllerDupe = new TypeMapping ("ViewController", "ViewController", null, false, false, false, null, null, new HashSet<string> ());
		typeService.AddType (viewControllerDupe);
		var result = typeService.AddType (viewControllerDupe);
		Assert.Null (result);
	}

	[Fact]
	public void AddDuplicateObjCType ()
	{
		var appDelegateDupe = new TypeMapping ("NewAppDelegate", "AppDelegate", null, false, false, false, null, null, new HashSet<string> ());
		typeService.AddType (appDelegateDupe);
		var result = typeService.AddType (appDelegateDupe);
		Assert.Null (result);
	}
}
