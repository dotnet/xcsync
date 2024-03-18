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
		Assert.Equal ("Invalid target framework 'net8.0' in csproj", OptionValidations.PathContainsValidTfm (Path.Combine (pwd, "xcsync.tests.csproj")));

	[Fact]
	public void PathContainsInvalidTfm2 () =>
		Assert.Equal ($"Path '{Path.GetTempPath ()}' does not contain a C# project", OptionValidations.PathContainsValidTfm (Path.GetTempPath ()));

	[Fact]
	public void PathContainsValidTfm () =>
		Assert.Null (OptionValidations.PathContainsValidTfm (Path.Combine (pwd, "Resources", "Valid.csproj")));

	[Theory]
	[InlineData ("net8.0-macos", null)]
	[InlineData ("net7.0", "Invalid target framework 'net7.0' in csproj")]
	[InlineData ("net7.0-maccatalyst", null)]
	[InlineData ("net6.0-ios", "Invalid target framework 'net6.0-ios' in csproj")]
	public void IsTfmValid (string tfm, string? error) =>
		Assert.Equal (error, tfm.IsValid ());
}