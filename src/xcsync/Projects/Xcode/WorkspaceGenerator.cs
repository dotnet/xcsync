// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

class XcodeWorkspaceGenerator {
	public static void Generate (IFileSystem fileSystem, string projectName, string username, string projectPath, XcodeProject? xcodeProject)
	{
		var projectBundle = CreateFolder (fileSystem, $"{projectName}.xcodeproj", projectPath);
		var workspaceBundle = CreateFolder (fileSystem, "project.xcworkspace", projectBundle);
		var xcuserdata = CreateFolder (fileSystem, "xcuserdata", workspaceBundle);
		var xcuserdatad = CreateFolder (fileSystem, $"{username}.xcuserdatad", xcuserdata);

		GenerateWorkspaceSettingsFile (fileSystem, xcuserdatad);
		GenerateWorkspaceDataFile (fileSystem, $"{projectName}.xcodeproj", workspaceBundle);
		GenerateXcodeProjectFile (fileSystem, projectBundle, xcodeProject);
	}

	static void GenerateWorkspaceSettingsFile (IFileSystem fileSystem, string path)
	{
		var workspaceSettingsTemplate = new WorkspaceSettings ();
		var workspaceSettings = workspaceSettingsTemplate.TransformText ();
		var workspaceSettingsPath = fileSystem.Path.Combine (path, "WorkspaceSettings.xcsettings");
		fileSystem.File.WriteAllText (workspaceSettingsPath, workspaceSettings);
	}

	static void GenerateWorkspaceDataFile (IFileSystem fileSystem, string projectName, string path)
	{
		var workspaceDataTemplate = new WorkspaceData (projectName);
		var workspaceData = workspaceDataTemplate.TransformText ();
		var workspaceDataPath = fileSystem.Path.Combine (path, "contents.xcworkspacedata");
		fileSystem.File.WriteAllText (workspaceDataPath, workspaceData);
	}

	static void GenerateXcodeProjectFile (IFileSystem fileSystem, string projectPath, XcodeProject? xcodeProject)
	{
		ArgumentNullException.ThrowIfNull (xcodeProject);

		var jsonString = JsonSerializer.Serialize (xcodeProject, new JsonSerializerOptions {
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		});
		var xcodeProjectPath = fileSystem.Path.Combine (projectPath, "project.pbxproj");
		fileSystem.File.WriteAllText (xcodeProjectPath, jsonString);
	}

	static string CreateFolder (IFileSystem fileSystem, string folderName, string parentPath)
	{
		var folderPath = fileSystem.Path.Combine (parentPath, folderName);
		if (!fileSystem.Directory.Exists (folderPath))
			fileSystem.Directory.CreateDirectory (folderPath);
		return folderPath;
	}
}
