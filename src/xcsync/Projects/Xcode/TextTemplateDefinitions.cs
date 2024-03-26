// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Projects.Xcode;

public partial class WorkspaceData : ITextTemplate {
	public WorkspaceData (string projectPath)
	{
		var thisTemplate = this as ITextTemplate;
		thisTemplate.Session = new Dictionary<string, object> { { "ProjectPath", projectPath } };
		thisTemplate.Initialize ();
	}
}

public partial class WorkspaceSettings : ITextTemplate {
}
