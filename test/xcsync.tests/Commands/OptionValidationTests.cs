using xcsync.Commands;

namespace xcsync.tests.Commands;

public class OptionValidationTests {
	// pwd ==  xcsync/xcsync.tests
	static readonly string pwd = Path.Combine (Environment.CurrentDirectory, "..", "..", "..");

	[Fact]
	public void EmptyPathName () =>
		Assert.Equal ("Path name is empty", OptionValidations.PathNameValid ("  "));

	[Fact]
	public void FileDoesNotExist ()
	{
		string path = Path.Combine (Path.GetTempPath (), "file-should-not-exist");
		File.Delete (path);
		Assert.Equal ($"Path '{path}' does not exist",
			OptionValidations.PathExists (path));
	}

	[Fact]
	public void DirectoryPathExists () =>
		Assert.Null (OptionValidations.PathExists (Path.GetTempPath ()));

	[Fact]
	public void DirectoryPathIsNotEmpty () =>
		Assert.Equal ($"Path '{Path.GetTempPath ()}' is not empty", OptionValidations.PathIsEmpty (Path.GetTempPath ()));

	[Fact]
	public void PathIsEmpty ()
	{
		string tempDirectoryPath = Path.Combine (Path.GetTempPath (), "testing-path-is-empty");
		Directory.CreateDirectory (tempDirectoryPath);
		File.Create (Path.Combine (tempDirectoryPath, "file-should-not-exist"));

		Assert.NotNull (OptionValidations.PathIsEmpty (tempDirectoryPath));
		Assert.Null (OptionValidations.PathCleaned (tempDirectoryPath));
		Assert.Null (OptionValidations.PathIsEmpty (tempDirectoryPath));

		Directory.Delete (tempDirectoryPath, true);
	}

	[Fact]
	public void PathContainsInvalidTfm () =>
		Assert.Equal ("Invalid target framework(s) 'net8.0' in csproj", OptionValidations.PathContainsValidTfm (Path.Combine (pwd, "xcsync.tests.csproj")));

	[Fact]
	public void PathContainsInvalidTfm2 () =>
		Assert.Equal ($"Path '{Path.GetTempPath ()}' does not contain a C# project", OptionValidations.PathContainsValidTfm (Path.GetTempPath ()));

	[Fact]
	public void PathContainsValidTfm () =>
		Assert.Null (OptionValidations.PathContainsValidTfm (Path.Combine (pwd, "Resources", "Valid.csproj")));

	[Fact]
	public void PathContainsValidMultipleTfms () =>
		Assert.Null (OptionValidations.PathContainsValidTfm (Path.Combine (pwd, "Resources", "MultipleValid.csproj")));

	[Theory]
	[InlineData ("net8.0-macos", null)]
	[InlineData("net8.0-macos14.0", null)]
	[InlineData ("net7.0", "Invalid target framework(s) 'net7.0' in csproj")]
	[InlineData ("net7.0-maccatalyst", null)]
	[InlineData ("net6.0-ios", null)]
	[InlineData ("net8.0-ios17.2", null)]
	[InlineData ("net10.0-macos", null)]
	[InlineData ("net8.0-maccatalyst;net8.0-ios;net8.0-android", null)]
	public void IsTfmValid (string tfm, string? error)
	{
		// inline data for xunit does not take in lists as a parameter, so we have to split the string
		var tfms = tfm.Split (';').ToList ();
		Assert.Equal (error, OptionValidations.IsTfmValid (ref tfms));
	}

	[Fact]
	public void ValidTargetPlatformMultiTfms ()
	{
		List<string> ProjectTfms = new () { "net8.0-macos", "net8.0-ios", "net8.0-maccatalyst" };
		GenerateCommand.TryGetTargetPlatform ("net8.0-ios", ProjectTfms, out string? targetPlatform);
		Assert.Equal ("ios", targetPlatform);
	}

	[Fact]
	public void InvalidTargetPlatformMultiTfms ()
	{
		List<string> ProjectTfms = new () { "net8.0-ios", "net8.0-maccatalyst" };
		GenerateCommand.TryGetTargetPlatform ("net8.0-macos", ProjectTfms, out string? targetPlatform);
		Assert.Null (targetPlatform);
	}

	[Fact]
	public void SpecifyTfmError ()
	{
		List<string> ProjectTfms = new () { "net8.0-ios", "net8.0-maccatalyst", "net8.0-android" };
		GenerateCommand.TryGetTargetPlatform ("", ProjectTfms, out string? targetPlatform);
		Assert.Null (targetPlatform);
	}

	[Fact]
	public void UseDefaultTfmIfSingle ()
	{
		List<string> ProjectTfms = new () { "net8.0-ios" };
		GenerateCommand.TryGetTargetPlatform ("", ProjectTfms, out string? targetPlatform);
		Assert.Equal ("ios", targetPlatform);
	}
}
