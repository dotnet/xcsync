// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serilog;
using xcsync.Ast;
using xcsync.Projects.Xcode;
using static ClangSharp.Interop.CXDiagnosticDisplayOptions;
using static ClangSharp.Interop.CXDiagnosticSeverity;
using static ClangSharp.Interop.CXErrorCode;
using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace xcsync.Projects;

partial class XcodeWorkspace (IFileSystem fileSystem, ILogger logger, ITypeService typeService, string name, string projectPath, string framework) :
	SyncableProject (fileSystem, logger, typeService, name, projectPath, framework, ["*.xcodeproj", "*.xcworkspace", "*.m", "*.h", "*.storyboard"]) {

	static CXIndex cxIndex = CXIndex.Create ();

	readonly CXTranslationUnit_Flags defaultTranslationUnitFlags = CXTranslationUnit_None
	| CXTranslationUnit_IncludeAttributedTypes      // Include attributed types in CXType
	| CXTranslationUnit_VisitImplicitAttributes;    // Implicit attributes should be visited;

	readonly List<string> clangCommandLineArgs = ["-x", "objective-c"];

	readonly JsonSerializerOptions jsonOptions = new () {
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	XcodeProject? Project { get; set; }

	string SdkRoot { get; set; } = string.Empty;

	readonly ConcurrentBag<ISyncableItem> syncableItems = [];

	// protected override async void InitializeAsync ()
	// {
	// 	await LoadAsync ().ConfigureAwait (false);
	// }

	public async Task LoadAsync (CancellationToken cancellationToken = default)
	{
		// Load the project files
		Project = await LoadProjectAsync (FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj"), cancellationToken).ConfigureAwait (false);

		if (Project is null) {
			Logger.Error (Strings.XcodeWorkspace.FailToLoadXcodeProject (FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj")));
			return;
		}

		if (Project.Objects is null) {
			Logger.Error (Strings.XcodeWorkspace.XcodeProjectDoesNotContainObjects (FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj")));
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

		clangCommandLineArgs.AddRange ([
			"-target",
			$"arm64-apple-{value}",
			"-isysroot",
			FileSystem.Path.Combine (Scripts.SelectXcode (), "Contents", "Developer", "Platforms", $"{SdkRoot}.platform", "Developer", "SDKs", $"{SdkRoot}.sdk"),
		]);

		await LoadSyncableItemsAsync (fileReferences, syncableItems, cancellationToken).ConfigureAwait (false);
	}

	async Task LoadSyncableItemsAsync (IEnumerable<PBXFileReference> fileReferences, ConcurrentBag<ISyncableItem> syncableItems, CancellationToken cancellationToken = default)
	{
		(from fileReference in fileReferences
		 where fileReference.Path!.EndsWith (".storyboard")
		 select new SyncableContent (FileSystem.Path.Combine (RootPath, fileReference.Path!)))
	   .ToList ().ForEach (syncableItems.Add);

		var filePaths = from moduleReference in fileReferences
						join headerReference in fileReferences on FileSystem.Path.GetFileNameWithoutExtension (moduleReference.Path) equals FileSystem.Path.GetFileNameWithoutExtension (headerReference.Path)
						where headerReference.Path!.EndsWith (".h") && moduleReference.Path!.EndsWith (".m")
						select moduleReference.Path;

		await new SyncableFiles (this, filePaths, Logger, cancellationToken).ExecuteAsync ().ConfigureAwait (false);
	}

	internal void ProcessObjCTypes (object? sender, NotifyCollectionChangedEventArgs e)
	{
		List<Task> processObjCTypeTasks = [];

		var items = e.NewItems ?? throw new ArgumentNullException (nameof (e));

		foreach (var type in items) {
			if (type is ObjCImplementationDecl objcType) { // This should always be true, but just in case
				Logger.Information (Strings.XcodeWorkspace.ProcessingObjCImplementation (objcType.Name));
				var types = TypeService.QueryTypes (null, objcType.Name);
				if (!types.Any ()) {
					Logger.Warning (Strings.XcodeWorkspace.NoTypesFound (objcType.Name));
				} else if (types.Count () > 1) {
					Logger.Warning (Strings.XcodeWorkspace.MultipleTypesFound (objcType.Name));
				} else {
					var typeMap = types.First ();
					if (typeMap is not null)
						processObjCTypeTasks.Add (UpdateRoslynType (typeMap, objcType));
				}

			} else {
				Logger.Warning (Strings.XcodeWorkspace.NoTypesFound (type?.ToString () ?? "null"));
			}
		}

		Task.WaitAll ([.. processObjCTypeTasks]);
	}

	internal async Task UpdateRoslynType (TypeMapping typeMap, ObjCImplementationDecl objcType)
	{
		try {
			SyntaxTree? syntaxTree = null;
			foreach (var syntaxRef in typeMap.TypeSymbol!.DeclaringSyntaxReferences) {
				if (!typeMap.InDesigner) {
					syntaxTree = syntaxRef.SyntaxTree;
					break;
				} else if (syntaxRef.SyntaxTree.FilePath.EndsWith ("designer.cs")) {
					syntaxTree = syntaxRef.SyntaxTree;
					break;
				}
			}
			if (syntaxTree is null) return;

			var rewriter = new ObjCSyntaxRewriter (Logger, TypeService, new AdhocWorkspace ());

			var newClass = await rewriter.WriteAsync (objcType.ClassInterface, syntaxTree);
			var root = syntaxTree!.GetRoot ();

			var oldClassNode = root!.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();
			var newClassNode = newClass!.GetRoot ().DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();

			var newRoot = root.ReplaceNode (oldClassNode, newClassNode
				.WithLeadingTrivia (SyntaxFactory.Whitespace (Environment.NewLine))); // This adds the blank line between the namespace and the class declaration

			await TypeService.TryUpdateMappingAsync (typeMap, newRoot).ConfigureAwait (false);
		} catch (Exception e) {
			Logger.Error (e, Strings.XcodeWorkspace.ErrorUpdatingRoslynType (nameof (UpdateRoslynType), typeMap.ObjCType, e.Message, e.StackTrace!));
		}
	}

	internal void LoadObjCTypesFromFiles (IEnumerable<string> filePaths, AstVisitor visitor, CancellationToken cancellationToken = default)
	{
		List<Task> loadTypesFromFileTasks = [];

		foreach (var filePath in filePaths) {
			loadTypesFromFileTasks.Add (LoadObjCTypesFromFileAsync (FileSystem.Path.Combine (RootPath, filePath), visitor, cancellationToken));
		}

		Task.WaitAll (loadTypesFromFileTasks.ToArray (), cancellationToken);
	}

	async Task LoadObjCTypesFromFileAsync (string filePath, AstVisitor visitor, CancellationToken cancellationToken = default)
	{
		var translationUnitError = CXTranslationUnit.TryParse (cxIndex, filePath, clangCommandLineArgs.ToArray (), [], defaultTranslationUnitFlags, out var handle);
		var skipProcessing = false;

		if (translationUnitError != CXError_Success) {
			Logger.Error (Strings.XcodeWorkspace.ErrorParsing (filePath, translationUnitError.ToString ()));
			skipProcessing = true;
		} else if (handle.NumDiagnostics != 0) {
			Logger.Warning (Strings.XcodeWorkspace.FileDiagnostics (filePath));

			for (uint i = 0; i < handle.NumDiagnostics; ++i) {
				using var diagnostic = handle.GetDiagnostic (i);

				if (diagnostic.Severity is CXDiagnostic_Error or CXDiagnostic_Fatal) {
					Logger.Error (Strings.XcodeWorkspace.DiagnosticIssue (diagnostic.Format (CXDiagnostic_DisplayOption).ToString ()));
				} else {
					Logger.Warning (Strings.XcodeWorkspace.DiagnosticIssue (diagnostic.Format (CXDiagnostic_DisplayOption).ToString ()));
				}
				// skipProcessing |= diagnostic.Severity == CXDiagnostic_Error;
				// skipProcessing |= diagnostic.Severity == CXDiagnostic_Fatal;
			}
		}

		if (skipProcessing) {
			Logger.Warning (Strings.XcodeWorkspace.SkipProcessing (filePath));
			return;
		}
		try {
			using var translationUnit = TranslationUnit.GetOrCreate (handle);
			Debug.Assert (translationUnit is not null);

			Logger.Information (Strings.XcodeWorkspace.ProcessingFile (filePath));

			var walker = new AstWalker ();
			await walker.WalkAsync (translationUnit.TranslationUnitDecl, visitor, (cursor) => {
				return !cursor.Location.IsInSystemHeader;
			}).ConfigureAwait (false);

		} catch (Exception e) {
			Logger.Error (e, Strings.XcodeWorkspace.ErrorProcessing (filePath, e.Message, e.StackTrace!));
		}
	}

	public async Task SaveProjectAsync (string path, CancellationToken cancellationToken = default)
	{
		using var json = FileSystem.File.OpenWrite (path);

		await JsonSerializer.SerializeAsync (json, Project, jsonOptions, cancellationToken).ConfigureAwait (false);
	}

	public async Task<XcodeProject?> LoadProjectAsync (string path, CancellationToken cancellationToken = default)
	{
		using var json = FileSystem.File.Open (path, FileMode.Open);

		return await JsonSerializer.DeserializeAsync<XcodeProject> (json, jsonOptions, cancellationToken).ConfigureAwait (false);
	}
}
