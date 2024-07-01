// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace xcsync.Projects;

interface ITypeService {
	void AddCompilation (string targetPlatform, Compilation compilation);
	TypeMapping? AddType (TypeMapping newType);
	IEnumerable<TypeMapping?> QueryTypes (string? clrType = null, string? objcType = null);
	Task<bool> TryUpdateMappingAsync (TypeMapping typeMap, SyntaxNode updatedMap);
}
