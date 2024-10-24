// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Serilog;

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

	public XcodeProjectBuilder WithDirectory (string directory)
	{
		var projectPath = new FilePath (directory);
		if (projectPath.Extension != ".xcodeproj")
			throw new ArgumentException ("An Xcode project folder must have a .xcodeproj extensino.", nameof (directory));

		this.projectPath = projectPath;
		return this;
	}

	public Model.XcodeProject Build ()
	{
		if (projectPath.IsNull)
			throw new DirectoryNotFoundException ("A directory must be provided to build an Xcode project.");

		return new Model.XcodeProject (projectPath);
	}
}