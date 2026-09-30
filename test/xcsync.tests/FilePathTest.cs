// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace xcsync.tests;

public class FilePathTest {
	[Fact]
	public void FilePath_EmptyPath_IsNull ()
	{
		var filePath = new FilePath (null);
		Assert.True (filePath.IsNull);
	}

	[Fact]
	public void FilePath_ValidPath_IsNotNull ()
	{
		var filePath = new FilePath ("/some/path");
		Assert.False (filePath.IsNull);
	}

	[Fact]
	public void FullPath_ReturnsCorrectFullPath ()
	{
		var filePath = new FilePath ("/some/path");
		Assert.Equal (Path.GetFullPath ("/some/path"), filePath.FullPath);
	}

	[Fact]
	public void DirectoryExists_IfDirectoryExists_ReturnsTrue ()
	{
		var tempDir = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
		Directory.CreateDirectory (tempDir);
		var filePath = new FilePath (tempDir);
		Assert.True (filePath.DirectoryExists);
		Directory.Delete (tempDir);
	}

	[Fact]
	public void FileExists_IfFileExists_ReturnsTrue ()
	{
		var tempFile = Path.GetTempFileName ();
		var filePath = new FilePath (tempFile);
		Assert.True (filePath.FileExists);
		File.Delete (tempFile);
	}

	[Fact]
	public void ParentDirectory_ReturnsCorrectParentDirectory ()
	{
		var filePath = new FilePath ("/some/path/file.txt");
		Assert.Equal (new FilePath ("/some/path"), filePath.ParentDirectory);
	}

	[Fact]
	public void Extension_ReturnsCorrectExtension ()
	{
		var filePath = new FilePath ("/some/path/file.txt");
		Assert.Equal (".txt", filePath.Extension);
	}

	[Fact]
	public void Name_ReturnsCorrectFileName ()
	{
		var filePath = new FilePath ("/some/path/file.txt");
		Assert.Equal ("file.txt", filePath.Name);
	}

	[Fact]
	public void NameWithoutExtension_ReturnsCorrectFileNameWithoutExtension ()
	{
		var filePath = new FilePath ("/some/path/file.txt");
		Assert.Equal ("file", filePath.NameWithoutExtension);
	}

	[Fact]
	public void CreateDirectory_CreatesDirectory ()
	{
		var tempDir = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
		var filePath = new FilePath (tempDir);
		filePath.CreateDirectory ();
		Assert.True (Directory.Exists (tempDir));
		Directory.Delete (tempDir);
	}

	[Fact]
	public void EnumerateDirectories_ReturnsDirectories ()
	{
		var tempDir = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
		Directory.CreateDirectory (tempDir);
		Directory.CreateDirectory (Path.Combine (tempDir, "subdir"));
		var filePath = new FilePath (tempDir);
		var directories = filePath.EnumerateDirectories ();
		Assert.Contains (new FilePath (Path.Combine (tempDir, "subdir")), directories);
		Directory.Delete (tempDir, true);
	}

	[Fact]
	public void EnumerateFiles_ReturnsFiles ()
	{
		var tempDir = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ());
		Directory.CreateDirectory (tempDir);
		var tempFile = Path.Combine (tempDir, "file.txt");
		File.Create (tempFile).Dispose ();
		var filePath = new FilePath (tempDir);
		var files = filePath.EnumerateFiles ();
		Assert.Contains (new FilePath (tempFile), files);
		Directory.Delete (tempDir, true);
	}

	[Fact]
	public void ChangeExtension_ChangesFileExtension ()
	{
		var filePath = new FilePath ("/some/path/file.txt");
		var newFilePath = filePath.ChangeExtension (".md");
		Assert.Equal ("/some/path/file.md", newFilePath.ToString ());
	}

	[Fact]
	public void Combine_CombinesPaths ()
	{
		var filePath = new FilePath ("/some/path");
		var combinedPath = filePath.Combine ("subdir", "file.txt");
		Assert.Equal ("/some/path/subdir/file.txt", combinedPath.ToString ());
	}

	[Fact]
	public void GetTempFileName_ReturnsTempFileName ()
	{
		var filePath = new FilePath ("/some/path");
		var tempFileName = filePath.GetTempFileName (".txt");
		Assert.EndsWith (".txt", tempFileName.ToString ());
	}

	[Fact]
	public void GetTempFileStream_ReturnsTempFileStream ()
	{
		var filePath = new FilePath ("/some/path");
		using var tempFileStream = filePath.GetTempFileStream (".txt");
		Assert.NotNull (tempFileStream);
		Assert.EndsWith (".txt", tempFileStream.FileName.ToString ());
	}

	[Fact]
	public void Equals_ReturnsTrueForEqualPaths ()
	{
		var filePath1 = new FilePath ("/some/path");
		var filePath2 = new FilePath ("/some/path");
		Assert.True (filePath1.Equals (filePath2));
	}

	[Fact]
	public void GetHashCode_ReturnsSameHashCodeForEqualPaths ()
	{
		var filePath1 = new FilePath ("/some/path");
		var filePath2 = new FilePath ("/some/path");
		Assert.Equal (filePath1.GetHashCode (), filePath2.GetHashCode ());
	}
}
