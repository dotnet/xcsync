// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Serilog;
using Xamarin.MacDev;
using xcsync.Projects.Xcode;
using Xunit.Abstractions;

namespace xcsync.tests.Projects;

public class XcodeProjectBuilderTest (ITestOutputHelper TestOutput) : Base {

	const string GuidValue1 = "1234567890ABCDEF1234567890ABCDEF";
	const string GuidValue2 = "ABCDEF1234567890ABCDEF1234567890";

	const string TestXcodeProjectPath = "/User/Home/someuser/source/TempProjectName/TempProjectName.xcodeproj";

	readonly ILogger testLogger = new LoggerConfiguration ()
	.MinimumLevel.Verbose ()
	.WriteTo.TestOutput (TestOutput)
	.CreateLogger ();

	[Theory]
	[InlineData ("")]
	[InlineData ("/path/toproject")]
	public void WithDirectory_WithInvalidDirectory_Throws (string directory)
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ());

		// Act
		void act () => builder.WithDirectory (directory);

		// Assert
		Assert.Throws<ArgumentException> ( act);
	}

	[Fact]
	public void WithDirectory_WithValidDirectory_DoesNotThrow ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ());

		// Act
		builder.WithDirectory (TestXcodeProjectPath);
		var project = builder.Build ();

		// Assert		
		Assert.NotNull (project);
		Assert.Equal (TestXcodeProjectPath, project.Path);
	}


	[Fact]
	public void Build_WithNoDirectory_ThrowsDirectoryNotFoundException ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ());

		// Act
		void act () => builder.Build ();

		// Assert
		Assert.Throws<DirectoryNotFoundException> ( act);
	}

	[Fact]
	public void Build_WithDirectory_ReturnsXcodeProjectFile ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ())
			.WithDirectory (TestXcodeProjectPath);

		// Act
		var project = builder.Build ();

		// Assert
		Assert.NotNull (project);
		Assert.Equal (TestXcodeProjectPath, project.Path);
		Assert.NotNull  (project.PbxProjectFile);
		Assert.Equal (TestXcodeProjectPath + "/project.pbxproj", project.PbxProjectFile.Filename);
	}

	[Fact]
	public void Build_UseArchiveVersion_SetsArchiveVersion ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ())
			.WithDirectory (TestXcodeProjectPath)
			.UseArchiveVersion (1);

		// Act
		var project = builder.Build ();

		// Assert
		Assert.NotNull (project);
		Assert.Equal (1, project.PbxProjectFile.ArchiveVersion);
	}

	[Fact]
	public void Build_UseObjectVersion_SetsObjectVersion ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ())
			.WithDirectory (TestXcodeProjectPath)
			.UseObjectVersion (1);

		// Act
		var project = builder.Build ();

		// Assert
		Assert.NotNull (project);
		Assert.Equal (1, project.PbxProjectFile.ObjectVersion);
	}

	[Fact]
	public void Build_WithoutCallAddApplication_AddsValidAppFileReference ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ())
			.WithDirectory (TestXcodeProjectPath);

		// Act
		var project = builder.Build ();
		var app = project.PbxProjectFile.Objects.FirstOrDefault (o =>
			 o.Properties ["name"] is PString { Value: "TempProjectName.app" }
		);

		// Assert
		Assert.NotNull (project);
		Assert.NotNull (app);
	}

	[Fact]
	public void Build_WithAddApplication_AddsAppFileReferenceUsingNameAndGuid ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ())
			.WithDirectory (TestXcodeProjectPath)
			.AddApplication ("ProjectName", GuidValue1);

		// Act
		var project = builder.Build ();
		var app = project.PbxProjectFile.Objects.FirstOrDefault (o =>
			 o.Properties ["explicitFileType"] is PString { Value: "wrapper.application" }
		);

		// Assert
		Assert.NotNull (project);
		Assert.NotNull (app);
		Assert.Equal (GuidValue1, app.Guid);
		Assert.Equal ("ProjectName.app", (app.Properties ["name"] as PString)?.Value);
	}

	[Fact]
	public void AddApplication_CalledMoreThanOnce_ThrowsInvalidOperationException ()
	{
		// Arrange
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ())
			.WithDirectory (TestXcodeProjectPath)
			.AddApplication ("ProjectName", GuidValue1);

		// Act
		void act () => builder.AddApplication ("ProjectName2", GuidValue2);

		// Assert
		Assert.Throws<InvalidOperationException> (act);
	}
}