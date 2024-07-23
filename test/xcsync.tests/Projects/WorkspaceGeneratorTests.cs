// Copyright (c) Microsoft Corporation. All rights reserved.

using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xamarin;
using xcsync.Projects.Xcode;

namespace xcsync.tests.Projects;

public class XcodeWorkspaceGeneratorTests {
	readonly string testProjectName = "TestProject";
	readonly string testUsername = "TestUser";
	readonly string testProjectPath = Cache.CreateTemporaryDirectory ();

	[Fact]
	public void Generate_CreatesExpectedWorkspace ()
	{
		var fileSystem = new MockFileSystem ();

		// Arrange
		string testFilePath = Path.Combine (SolutionPathFinder.GetProjectRoot(), "xcsync.tests", "Resources", "SampleProject.json");
		string xcodeProjectJson = File.ReadAllText (testFilePath);
		xcodeProjectJson = xcodeProjectJson.Replace ("SampleProject", testProjectName);

		XcodeProject? xcodeProject = JsonSerializer.Deserialize<XcodeProject> (xcodeProjectJson, new JsonSerializerOptions {
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		});

		// Act
		XcodeWorkspaceGenerator.Generate (fileSystem, testProjectName, testUsername, testProjectPath, xcodeProject);

		// Assert
		Assert.True (fileSystem.File.Exists (fileSystem.Path.Combine (testProjectPath, $"{testProjectName}.xcodeproj", "project.xcworkspace", "xcuserdata", $"{testUsername}.xcuserdatad", "WorkspaceSettings.xcsettings")));
		Assert.True (fileSystem.File.Exists (fileSystem.Path.Combine (testProjectPath, $"{testProjectName}.xcodeproj", "project.xcworkspace", "contents.xcworkspacedata")));
		Assert.True (fileSystem.File.Exists (fileSystem.Path.Combine (testProjectPath, $"{testProjectName}.xcodeproj", "project.pbxproj")));
	}

	[Fact]
	public void WorkspaceSettingsTransformText_GeneratesWorkspaceSettingsFile ()
	{
		var generated = new WorkspaceSettings ().TransformText ();

		const string expected =
			"""
			<?xml version=""1.0"" encoding=""UTF-8""?>
			<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
			<plist version=""1.0"">
			<dict>
				<key>BuildLocationStyle</key>
				<string>UseAppPreferences</string>
				<key>CustomBuildIntermediatesPath</key>
				<string>Build/Intermediates.noindex</string>
				<key>CustomBuildLocationType</key>
				<string>RelativeToDerivedData</string>
				<key>CustomBuildProductsPath</key>
				<string>Build/Products</string>
				<key>CustomIndexStorePath</key>
				<string>Index/DataStore</string>
				<key>DerivedDataCustomLocation</key>
				<string>DerivedData</string>
				<key>DerivedDataLocationStyle</key>
				<string>WorkspaceRelativePath</string>
				<key>EnabledFullIndexStoreVisibility</key>
				<false/>
				<key>IDEWorkspaceUserSettings_BuildLocationStyle</key>
				<integer>0</integer>
				<key>IDEWorkspaceUserSettings_BuildSubfolderNameStyle</key>
				<integer>0</integer>
				<key>IDEWorkspaceUserSettings_DerivedDataCustomLocation</key>
				<string>DerivedData</string>
				<key>IDEWorkspaceUserSettings_DerivedDataLocationStyle</key>
				<integer>2</integer>
				<key>IDEWorkspaceUserSettings_IssueFilterStyle</key>
				<integer>0</integer>
				<key>IDEWorkspaceUserSettings_LiveSourceIssuesEnabled</key>
				<true/>
				<key>IDEWorkspaceUserSettings_SnapshotAutomaticallyBeforeSignificantChanges</key>
				<true/>
				<key>IDEWorkspaceUserSettings_SnapshotLocationStyle</key>
				<integer>0</integer>
				<key>IssueFilterStyle</key>
				<string>ShowActiveSchemeOnly</string>
				<key>LiveSourceIssuesEnabled</key>
				<true/>
				<key>SnapshotAutomaticallyBeforeSignificantChanges</key>
				<true/>
				<key>SnapshotLocationStyle</key>
				<string>Default</string>
			</dict>
			</plist>
			""";

		Assert.Equal (expected, generated);
	}

	[Fact]
	public void WorkspaceDataTransformText_GeneratesWorkspaceDataFile ()
	{
		var generated = new WorkspaceData (testProjectPath).TransformText ();

		string expected =
			$"""
			<?xml version="1.0" encoding="UTF-8"?>
			<Workspace
				version = "1.0">
				<FileRef
				location = "self:{testProjectPath}">
				</FileRef>
			</Workspace>
			""";

		Assert.Equal (expected, generated);
	}
}
