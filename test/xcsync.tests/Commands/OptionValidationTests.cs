using xcsync.Commands;

namespace xcsync.tests.Commands;

public class OptionValidationTests {
	// pwd ==  xcsync/xcsync.tests
	static readonly string pwd = Path.Combine (Environment.CurrentDirectory, "..", "..", "..");

	[Fact]
	public void EmptyPathName () =>
		Assert.Equal ("Path cannot be empty", OptionValidations.PathNameValid (new DirectoryInfo ("  ")));

	[Fact]
	public void PathDoesNotExist ()
	{
		DirectoryInfo path = new (Path.Combine (Path.GetTempPath (), "file-should-not-exist"));
		File.Delete (path.FullName);
		Assert.Equal ("Path does not exist",
			OptionValidations.PathExists (path));
	}

	[Fact]
	public void PathExists () =>
		Assert.Null (OptionValidations.PathExists (new DirectoryInfo (Path.GetTempPath ())));

	[Fact]
	public void PathIsNotEmpty () =>
		Assert.Equal ("Path is not empty", OptionValidations.PathIsEmpty (new DirectoryInfo (Path.GetTempPath ())));

	[Fact]
	public void PathIsEmpty ()
	{
		var path = Directory.CreateDirectory (Directory.GetCurrentDirectory ());
		Directory.CreateDirectory (path.FullName);

		Assert.NotNull (OptionValidations.PathIsEmpty (path));
		Assert.Null (OptionValidations.PathCleaned (path));
		Assert.Null (OptionValidations.PathIsEmpty (path));
	}

	[Fact]
	public void PathContainsInvalidTfm () =>
		Assert.Equal ("Invalid target framework in csproj", OptionValidations.PathContainsValidTfm (new DirectoryInfo (pwd)));

	[Fact]
	public void PathContainsInvalidTfm2 () =>
		Assert.Equal ("Path does not contain a C# project", OptionValidations.PathContainsValidTfm (new DirectoryInfo (Path.GetTempPath ())));

	[Fact]
	public void PathContainsValidTfm () =>
		Assert.Null (OptionValidations.PathContainsValidTfm (new DirectoryInfo (Path.Combine (pwd, "Resources"))));

	[Theory]
	[InlineData ("net8.0-macos", null)]
	[InlineData ("net7.0", "Invalid target framework in csproj")]
	[InlineData ("net7.0-maccatalyst", null)]
	[InlineData ("net6.0-ios", "Invalid target framework in csproj")]
	public void IsTfmValid (string tfm, string? error) =>
		Assert.Equal (error, OptionValidations.IsTfmValid (tfm));
}
