// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Projects;

public class NSObject (string cliName, string objcName, NSObject? baseType, bool isModel, bool inDesigner, List<IBOutlet>? outlets, List<IBAction>? actions) {

	public string CliType { get; set; } = cliName;
	public string ObjCType { get; set; } = objcName;
	public NSObject? BaseType { get; set; } = baseType;
	public bool IsModel { get; set; } = isModel;
	public bool InDesigner { get; set; } = inDesigner;
	public List<IBOutlet>? Outlets { get; set; } = outlets;
	public List<IBAction>? Actions { get; set; } = actions;
}

public class IBOutlet (string cliName, string objcName, string cliType, string? objcType, bool isCollection) {
	public string CliName { get; set; } = cliName;
	public string ObjCName { get; set; } = objcName;
	public string CliType { get; set; } = cliType;
	public string? ObjCType { get; set; } = objcType;
	public bool IsCollection { get; set; } = isCollection;
}

public class IBAction (string cliName, string objcName, List<IBActionParameter> parameters) {
	public string CliName { get; set; } = cliName;
	public string ObjCName { get; set; } = objcName;
	public List<IBActionParameter> Parameters { get; set; } = parameters;
}

public class IBActionParameter (string cliName, string? objcName, string cliType, string? objcType) {
	public string CliName { get; set; } = cliName;
	public string? ObjCName { get; set; } = objcName;
	public string CliType { get; set; } = cliType;
	public string? ObjCType { get; set; } = objcType;
}
