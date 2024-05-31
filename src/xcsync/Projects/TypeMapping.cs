// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.CodeAnalysis;

namespace xcsync.Projects;

record TypeMapping (INamedTypeSymbol TypeSymbol, string CliType, string ObjCType, TypeMapping? BaseType, bool IsModel, bool IsProtocol,
	bool InDesigner, List<IBOutlet>? Outlets, List<IBAction>? Actions, HashSet<string> References);

class IBOutlet (string cliName, string objcName, string cliType, string? objcType, bool isCollection) {
	public string CliName { get; set; } = cliName;
	public string ObjCName { get; set; } = objcName;
	public string CliType { get; set; } = cliType;
	public string? ObjCType { get; set; } = objcType;
	public bool IsCollection { get; set; } = isCollection;
}

class IBAction (string cliName, string objcName, List<IBActionParameter> parameters) {
	public string CliName { get; set; } = cliName;
	public string ObjCName { get; set; } = objcName;
	public List<IBActionParameter> Parameters { get; set; } = parameters;
}

class IBActionParameter (string cliName, string? objcName, string cliType, string? objcType) {
	public string CliName { get; set; } = cliName;
	public string? ObjCName { get; set; } = objcName;
	public string CliType { get; set; } = cliType;
	public string? ObjCType { get; set; } = objcType;
}
