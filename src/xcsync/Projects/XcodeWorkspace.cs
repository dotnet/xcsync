// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Serilog;
using xcsync.Projects.Xcode;
using static ClangSharp.Interop.CXDiagnosticDisplayOptions;
using static ClangSharp.Interop.CXDiagnosticSeverity;
using static ClangSharp.Interop.CXErrorCode;
using static ClangSharp.Interop.CXTranslationUnit_Flags;
namespace xcsync.Projects;

partial class XcodeWorkspace (IFileSystem fileSystem, ILogger logger, string name, string projectPath, string framework) :
	SyncableProject (fileSystem, logger, name, projectPath, framework, ["*.xcodeproj", "*.xcworkspace", "*.m", "*.h", "*.storyboard"]) {

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
	readonly ObservableCollection<ObjCImplementationDecl> objcTypes = [];

	protected override async void InitializeAsync ()
	{
		await LoadAsync ();
	}

	public async Task LoadAsync (CancellationToken cancellationToken = default)
	{
		// Load the project files
		Project = await LoadProjectAsync (FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj"), cancellationToken);

		if (Project is null) {
			Logger.Error ("Failed to load the Xcode project file at {ProjectPath}", FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj"));
			return;
		}

		if (Project.Objects is null) {
			Logger.Error ("The Xcode project file at {ProjectPath} does not contain any objects", FileSystem.Path.Combine (RootPath, $"{Name}.xcodeproj", "project.pbxproj"));
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

		await LoadSyncableItemsAsync (fileReferences, syncableItems, cancellationToken);
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

		var visitor = new ObjCImplementationDeclVisitor (Logger);
		visitor.ObjCTypes.CollectionChanged += ProcessObjCTypes;
		await LoadObjCTypesFromFilesAsync (filePaths, visitor, cancellationToken);
		visitor.ObjCTypes.CollectionChanged -= ProcessObjCTypes;
	}

	void ProcessObjCTypes (object? sender, NotifyCollectionChangedEventArgs e)
	{
		var items = e.NewItems ?? throw new ArgumentNullException (nameof (e));

		foreach (var type in items) {
			if (type is ObjCImplementationDecl objcType) { // This should always be true, but just in case
				Logger.Information ("Processing ObjCImplementationDecl: {objcType}", objcType.Name);
				// TODO : Update INamedSymbol of TypeMapping to match the ObjCImplementationDecl
				//		  Where the TypeMapping.ObjCType is the ObjCImplementationDecl.Name
			} else {
				Logger.Warning ("Unexpected type found: {type}", type);
			}
		}
	}

	internal async Task LoadObjCTypesFromFilesAsync (IEnumerable<string> filePaths, AstVisitor visitor, CancellationToken cancellationToken = default)
	{
		List<Task> loadTypesFromFileTasks = [];

		ConcurrentBag<ObjCImplementationDecl> types = [];
		foreach (var filePath in filePaths) {
			loadTypesFromFileTasks.Add (LoadObjCTypesFromFileAsync (filePath, visitor, cancellationToken));
		}

		await Task.WhenAll (loadTypesFromFileTasks);
	}

	async Task LoadObjCTypesFromFileAsync (string filePath, AstVisitor visitor, CancellationToken cancellationToken = default)
	{
		var translationUnitError = CXTranslationUnit.TryParse (cxIndex, filePath, clangCommandLineArgs.ToArray (), [], defaultTranslationUnitFlags, out var handle);
		var skipProcessing = false;

		if (translationUnitError != CXError_Success) {
			Logger.Error ("Error: Parsing failed for '{filePath}' due to '{translationUnitError}'.", filePath, translationUnitError);
			skipProcessing = true;
		} else if (handle.NumDiagnostics != 0) {
			Logger.Warning ("Diagnostics for '{filePath}':", filePath);

			for (uint i = 0; i < handle.NumDiagnostics; ++i) {
				using var diagnostic = handle.GetDiagnostic (i);

				if (diagnostic.Severity is CXDiagnostic_Error or CXDiagnostic_Fatal) {
					Logger.Error ("{diagnostic}", diagnostic.Format (CXDiagnostic_DisplayOption).ToString ());
				} else {
					Logger.Warning ("{diagnostic}", diagnostic.Format (CXDiagnostic_DisplayOption).ToString ());
				}
				// skipProcessing |= diagnostic.Severity == CXDiagnostic_Error;
				// skipProcessing |= diagnostic.Severity == CXDiagnostic_Fatal;
			}
		}

		if (skipProcessing) {
			Logger.Warning ("Skipping '{filePath}' due to one or more errors listed above.", filePath);
			return;
		}
		try {
			using var translationUnit = TranslationUnit.GetOrCreate (handle);
			Debug.Assert (translationUnit is not null);

			Logger.Information ("Processing '{filePath}'", filePath);

			var walker = new AstWalker ();
			await walker.WalkAsync (translationUnit.TranslationUnitDecl, visitor, (cursor) => {
				return !cursor.Location.IsInSystemHeader;
			});

		} catch (Exception e) {
			Logger.Error (e, "Error processing '{filePath}'", filePath);
		}
	}

	public async Task SaveProjectAsync (string path, CancellationToken cancellationToken = default)
	{
		using var json = FileSystem.File.OpenWrite (path);

		await JsonSerializer.SerializeAsync (json, Project, jsonOptions, cancellationToken);
	}

	public async Task<XcodeProject?> LoadProjectAsync (string path, CancellationToken cancellationToken = default)
	{
		using var json = FileSystem.File.Open (path, FileMode.Open);

		return await JsonSerializer.DeserializeAsync<XcodeProject> (json, jsonOptions, cancellationToken);
	}

}
