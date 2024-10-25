// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Serilog;
using xcsync.Projects.Xcode.Model;

namespace xcsync.Projects.Xcode;

class XcodeProjectBuilder {
	public XcodeProjectBuilder (ILogger logger, IFileSystem fileSystem)
	{
		ArgumentNullException.ThrowIfNull (logger, nameof (logger));
		ArgumentNullException.ThrowIfNull (fileSystem, nameof (fileSystem));

		Logger = logger;
		FileSystem = fileSystem;
	}

	public ILogger Logger { get; }
	public IFileSystem FileSystem { get; }

	FilePath projectPath;
	int objectVersion;
	int archiveVersion;

	(string name, PbxGuid guid) app;

	public XcodeProjectBuilder WithDirectory (string directory)
	{
		var projectPath = new FilePath (directory);
		if (projectPath.Extension != ".xcodeproj")
			throw new ArgumentException ("An Xcode project folder must have a .xcodeproj extensino.", nameof (directory));

		this.projectPath = projectPath;
		return this;
	}
	
	public XcodeProjectBuilder UseObjectVersion (int version)
	{
		if (version < 0)
			throw new ArgumentOutOfRangeException (nameof (version), "The object version must be a positive integer.");
		
		objectVersion = version;
		return this;
	}

	public XcodeProjectBuilder UseArchiveVersion (int version)
	{
		if (version < 0)
			throw new ArgumentOutOfRangeException (nameof (version), "The object version must be a positive integer.");

		archiveVersion = version;
		return this;
	}

	public XcodeProjectBuilder AddApplication(string name, string guid = "")
	{
		if (app != default)
			throw new InvalidOperationException("An application has already been added to the project.");
			
		if (string.IsNullOrEmpty(name))
			throw new ArgumentException("An application name must be provided.", nameof(name));

		if (string.IsNullOrEmpty(guid))
			guid = PbxGuid.NewGuid().ToString();

		app = (name, new PbxGuid(guid));
		return this;
	}

	public Model.XcodeProject Build ()
	{
		if (projectPath.IsNull)
			throw new DirectoryNotFoundException ("A directory must be provided to build an Xcode project.");

		var xcodeProject = new Model.XcodeProject (projectPath);

		xcodeProject.PbxProjectFile.ArchiveVersion = archiveVersion;
		xcodeProject.PbxProjectFile.ObjectVersion = objectVersion;

		AddAppPbxObject (xcodeProject);

		return xcodeProject;
	}

	void AddAppPbxObject(Model.XcodeProject xcodeProject)
	{
		var projectName = this.projectPath.NameWithoutExtension;

		(string appName, PbxGuid appGuid) = this.app;

		if (string.IsNullOrEmpty(appName))
			appName = $"{projectName}";
		
		var app = new PbxFileReference (xcodeProject.PbxProjectFile, appGuid, new Xamarin.MacDev.PDictionary {
			{"isa", "PBXFileReference"},
			{ "explicitFileType", "wrapper.application" },
			{ "name", $"{appName}.app" },
			{ "path", $"{appName}.app" },
			{ "sourceTree", "BUILT_PRODUCTS_DIR" },
			{ "includeInIndex", "0" },
		});
		xcodeProject.PbxProjectFile.AddObject (app);
	}
}