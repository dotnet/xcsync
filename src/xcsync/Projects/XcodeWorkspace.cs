// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Serilog;
using xcsync.Projects.Xcode;

namespace xcsync.Projects;

partial class XcodeWorkspace (IFileSystem fileSystem, ILogger logger, string name, string projectPath, string framework)
	: SyncableProject (fileSystem, logger, name, projectPath, framework, ["*.xcodeproj", "*.xcworkspace", "*.m", "*.h", "*.storyboard"]) {

	readonly JsonSerializerOptions jsonOptions = new () {
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	XcodeProject? Project { get; set; }
	
	string SdkRoot { get; set; } = string.Empty;

	readonly ConcurrentBag<ISyncableItem> syncableItems = [];

	public async Task LoadAsync (CancellationToken cancellationToken = default)
	{
		IEnumerable<Task> loadingTasks = [];

		// Load the project files
		var Project = await LoadProjectAsync (FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj"), cancellationToken);

		if (Project is null) {
			Logger.Error ("Failed to load the Xcode project file at {ProjectPath}", FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj"));
			return;
		}

		if (Project.Objects is null) {
			Logger.Error ("The Xcode project file at {ProjectPath} does not contain any objects", FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj"));
			return;
		}

		var frameworkGroup = (from groups in Project.Objects.Values
							  where groups.Isa == "PBXGroup" && groups is PBXGroup { Name: "Frameworks" }
							  select groups as PBXGroup).First ().Children.AsQueryable ();

		var fileReferences = from fileRef in Project.Objects.Values
							 where fileRef.Isa == "PBXFileReference"
							 select fileRef as PBXFileReference;

		var frameworks = from frameworkRef in frameworkGroup
						 join fileRef in fileReferences on frameworkRef equals fileRef.Token
						 select fileRef;

		var configuration = (from configurations in Project.Objects.Values
							 where configurations.Isa == "XCBuildConfiguration" && configurations is XCBuildConfiguration { Name: "Release" } // TODO: Support Debug configuration?
							 select configurations as XCBuildConfiguration).First ();

		var buildSettings = configuration?.BuildSettings?.AsQueryable ();
		var value = buildSettings?
					.First (
						(v) => v.Key == "SDKROOT"
					).Value?.FirstOrDefault ();
		SdkRoot = (value ?? string.Empty) switch {
			"macosx" => "MacOSX",
			"iphoneos" => "iPhoneOS",
			_ => string.Empty
		};

		Task.WaitAll ([
			LoadSyncableItemsAsync (fileReferences, syncableItems, cancellationToken),
			], cancellationToken);
		return;
	}

	Task LoadSyncableItemsAsync (IEnumerable<PBXFileReference> fileReferences, ConcurrentBag<ISyncableItem> syncableItems, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}

	public async Task SaveProjectAsync (string path, CancellationToken cancellationToken = default)
	{
		using var json = FileSystem.File.OpenWrite (path);

		await JsonSerializer.SerializeAsync (json, Project, jsonOptions, cancellationToken);
	}

	public async Task<XcodeProject?> LoadProjectAsync (string path, CancellationToken cancellationToken = default)
	{
		using var json = FileSystem.File.Open (path, FileMode.Open);

		return await JsonSerializer.DeserializeAsync<XcodeProject> (json, jsonOptions, cancellationToken);
	}
}
