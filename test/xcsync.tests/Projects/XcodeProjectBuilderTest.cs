// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Serilog;
using xcsync.Projects.Xcode;
using Xunit.Abstractions;

namespace xcsync.tests.Projects;

public class XcodeProjectBuilderTest (ITestOutputHelper TestOutput) : Base {

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
		const string directory = "/User/Home/someuser/source/TempProjectName/TempProjectName.xcodeproj";
		var builder = new XcodeProjectBuilder (testLogger, new FileSystem ());

		// Act
		builder.WithDirectory (directory);
		var project = builder.Build ();

		// Assert		
		Assert.NotNull (project);
		Assert.Equal (directory, project.Path);
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

}