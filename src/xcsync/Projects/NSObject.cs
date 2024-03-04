// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync.Projects;

public class NSObject (string cliName, string objcName, NSObject? baseType, bool isModel, bool inDesigner) {

	public string CliType { get; set; } = cliName;
	public string ObjCType { get; set; } = objcName;
	public NSObject? BaseType { get; set; } = baseType;
	public bool IsModel { get; set; } = isModel;
	public bool InDesigner { get; set; } = inDesigner;

	// todo: implement in next PR
	// List<IBOutlet> Outlets { get; }
	// List<IBAction> Actions { get; }
}
