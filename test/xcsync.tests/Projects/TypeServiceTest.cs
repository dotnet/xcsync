using Microsoft.CodeAnalysis;
using Moq;
using xcsync.Projects;

namespace xcsync.tests.Projects;

public class TypeServiceTest {
	readonly TypeService typeService;

	public TypeServiceTest ()
	{
		typeService = new TypeService ();

		typeService.AddType (Mock.Of<INamedTypeSymbol> (),"ViewController", "ViewController", null, false, false, false, null, null, []);
		typeService.AddType (Mock.Of<INamedTypeSymbol> (), "AppDelegate", "AppDelegate", null, false, false, false, null, null, []);
		typeService.AddType (Mock.Of<INamedTypeSymbol> (), "TypeExtension", "TypeExtension", null, false, false, false, null, null, []);
	}

	[Fact]
	public void AddType ()
	{
		var type = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "NewType", "NewType", null, false, false, false, null, null, []);
		var result = typeService.AddType (type);
		Assert.NotNull (result);
		Assert.Equal (type, result);
	}

	[Fact]
	public void QueryExistentTypes ()
	{
		var type = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "NewTypeCli", "NewTypeObjC", null, false, false, false, null, null, []);
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
		var viewControllerDupe = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "ViewController", "ViewController", null, false, false, false, null, null, []);
		typeService.AddType (viewControllerDupe);
		var result = typeService.AddType (viewControllerDupe);
		Assert.Null (result);
	}

	[Fact]
	public void AddDuplicateObjCType ()
	{
		var appDelegateDupe = new TypeMapping (Mock.Of<INamedTypeSymbol> (), "NewAppDelegate", "AppDelegate", null, false, false, false, null, null, []);
		typeService.AddType (appDelegateDupe);
		var result = typeService.AddType (appDelegateDupe);
		Assert.Null (result);
	}
}
