// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace xcsync.Projects.Xcode.Model;

record class XcodeProject {
	public FilePath Path { get; }
	public PbxProjectFile PbxProjectFile { get; }

	public XcodeProject (FilePath path)
	{
		Path = path;

		var pbxprojFilename = this.Path.Combine ("project.pbxproj");

		PbxProjectFile = new PbxProjectFile (pbxprojFilename);
	}

};
