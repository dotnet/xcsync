// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.CodeAnalysis;

namespace xcsync.Projects;


record TypeMapping (INamedTypeSymbol? TypeSymbol, string ClrType, string ObjCType, TypeMapping? BaseType, bool IsModel, bool IsProtocol,
	bool InDesigner, List<IBOutlet>? Outlets, List<IBAction>? Actions, HashSet<string> References, bool IsInSource = false);

class IBOutlet (string clrName, string objcName, string clrType, string? objcType, bool isCollection) {
	public string ClrName { get; set; } = clrName;
	public string ObjCName { get; set; } = objcName;
	public string ClrType { get; set; } = clrType;
	public string? ObjCType { get; set; } = objcType;
	public bool IsCollection { get; set; } = isCollection;
}

class IBAction (string clrName, string objcName, List<IBActionParameter> parameters) {
	public string ClrName { get; set; } = clrName;
	public string ObjCName { get; set; } = objcName;
	public List<IBActionParameter> Parameters { get; set; } = parameters;
}

class IBActionParameter (string clrName, string? objcName, string clrType, string? objcType) {
	public string ClrName { get; set; } = clrName;
	public string? ObjCName { get; set; } = objcName;
	public string ClrType { get; set; } = clrType;
	public string? ObjCType { get; set; } = objcType;
}
