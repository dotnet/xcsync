// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace xcsync.Projects.Xcode;

public class XcodeWorkspaceGenerator {
	public static void Generate (string projectName, string username, string projectPath, XcodeProject? xcodeProject)
	{
		var projectBundle = CreateFolder ($"{projectName}.xcodeproj", projectPath);
		var workspaceBundle = CreateFolder ("project.xcworkspace", projectBundle);
		var xcuserdata = CreateFolder ("xcuserdata", workspaceBundle);
		var xcuserdatad = CreateFolder ($"{username}.xcuserdatad", xcuserdata);

		GenerateWorkspaceSettingsFile (xcuserdatad);
		GenerateWorkspaceDataFile ($"{projectName}.xcodeproj", workspaceBundle);
		GenerateXcodeProjectFile (projectBundle, xcodeProject);
	}

	static void GenerateWorkspaceSettingsFile (string path)
	{
		var workspaceSettingsTemplate = new WorkspaceSettings ();
		var workspaceSettings = workspaceSettingsTemplate.TransformText ();
		var workspaceSettingsPath = Path.Combine (path, "WorkspaceSettings.xcsettings");
		File.WriteAllText (workspaceSettingsPath, workspaceSettings);
	}

	static void GenerateWorkspaceDataFile (string projectName, string path)
	{
		var workspaceDataTemplate = new WorkspaceData (projectName);
		var workspaceData = workspaceDataTemplate.TransformText ();
		var workspaceDataPath = Path.Combine (path, "contents.xcworkspacedata");
		File.WriteAllText (workspaceDataPath, workspaceData);
	}

	static void GenerateXcodeProjectFile (string projectPath, XcodeProject? xcodeProject)
	{
		ArgumentNullException.ThrowIfNull (xcodeProject);

		var jsonString = JsonSerializer.Serialize (xcodeProject, new JsonSerializerOptions {
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		});
		var xcodeProjectPath = Path.Combine (projectPath, "project.pbxproj");
		File.WriteAllText (xcodeProjectPath, jsonString);
	}

	static string CreateFolder (string folderName, string parentPath)
	{
		var folderPath = Path.Combine (parentPath, folderName);
		if (!Directory.Exists (folderPath))
			Directory.CreateDirectory (folderPath);
		return folderPath;
	}
}
