// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
#nullable enable
namespace xcsync;

static class Strings {
	static string ProgramHeaderFormat => Resources.Strings.ProgramHeader;

	public static string ProgramHeader => string.Format (ProgramHeaderFormat, typeof (xcSync).Assembly.GetName ().Version);

	internal static class Options {
		internal static string ForceDescription => Resources.Strings.Options_Force_Description;
		internal static string ProjectDescription => Resources.Strings.Options_Project_Description;
		internal static string? ProjectValidationError (string error) => Errors.InvalidOption ("project", error);
		internal static string TargetDescription => Resources.Strings.Options_Target_Description;
		internal static string? TargetValidationError (string error) => Errors.InvalidOption ("target", error);
		internal static string OpenDescription => Resources.Strings.Options_Open_Description;
		internal static string TfmDescription => Resources.Strings.Options_Tfm_Description;
		internal static string VerbosityDescription => Resources.Strings.Options_Verbosity_Description;
		internal static string DotnetPathDescription => Resources.Strings.Options_DotnetPath_Description;
	}

	internal static class Commands {
		internal static string GenerateDescription => Resources.Strings.Commands_Generate_Description;
		internal static string SyncDescription => Resources.Strings.Commands_Sync_Description;
		internal static string WatchDescription => Resources.Strings.Commands_Watch_Description;
	}

	internal static class Errors {
		internal static class Validation {
			internal static string PathNameEmpty => Resources.Strings.Errors_Validation_PathNameIsEmpty;
			internal static string PathDoesNotExist (string path) => string.Format (Resources.Strings.Errors_Validation_PathDoesNotExist, path);
			internal static string TargetDoesNotExist (string path) => string.Format (Resources.Strings.Errors_Validation_TargetDoesNotExist, path);
			internal static string TargetNotEmpty (string path) => string.Format (Resources.Strings.Errors_Validation_TargetIsNotEmpty, path);
			internal static string PathDoesNotContainCsproj (string path) => string.Format (Resources.Strings.Errors_Validation_PathDoesNotContainCsproj, path);
			internal static string MissingTfmInPath (string path) => string.Format (Resources.Strings.Errors_Validation_MissingValidTargetFrameworkInPath, path);
			internal static string InvalidTfm (string tfm) => string.Format (Resources.Strings.Errors_Validation_InvalidTargetFramework, tfm);
			internal static string InvalidVerbosity => Resources.Strings.Errors_Validation_InvalidVerbosity;
			internal static string InvalidOS => Resources.Strings.Errors_Validation_InvalidOS;
		}

		internal static string InvalidOption (string option, string error) => string.Format (Resources.Strings.Errors_InvalidOption, option, error);
		internal static string MultipleProjectFilesFound (string path, string projectFilesFound) => string.Format (Resources.Strings.Errors_MultipleProjectFilesFound, path, projectFilesFound);
		internal static string CsprojNotFound (string path) => string.Format (Resources.Strings.Errors_CsprojNotFound, path);
		internal static string MultipleTfmsFound => Resources.Strings.Errors_MultipleTfmsFound;
		internal static string TargetPlatformNotFound => Resources.Strings.Errors_TargetPlatformNotFound;
		internal static string TfmNotSupported => Resources.Strings.Errors_TfmNotSupported;
	}

	internal static class Base {
		internal static string SearchForProjectFiles (string path) => string.Format (Resources.Strings.Base_SearchForProjectFiles, path);
		internal static string FoundProjectFile (string projectFile, string path) => string.Format (Resources.Strings.Base_FoundProjectFile, projectFile, path);
		internal static string ProjectTfms (string tfms) => string.Format (Resources.Strings.Base_ProjectTfms, tfms);
		internal static string UseDefaultTfm => Resources.Strings.Base_UseDefaultTfm;
		internal static string EstablishDefaultTarget (string path) => string.Format (Resources.Strings.Base_EstablishDefaultTarget, path);
		internal static string CreateDefaultTarget (string path) => string.Format (Resources.Strings.Base_CreateDefaultTarget, path);
	}
	internal static class Generate {
		internal static string HeaderInformation => Resources.Strings.Generate_HeaderInformation;
		internal static string GeneratedFiles => Resources.Strings.Generate_GeneratedFiles;
		internal static string GeneratedProject (string path) => string.Format (Resources.Strings.Generate_GeneratedProject, path);
		internal static string OpenProject (string path) => string.Format (Resources.Strings.Generate_OpenProject, path);
	}

	internal static class Sync {
		internal static string HeaderInformation => Resources.Strings.Sync_HeaderInformation;
	}

	internal static class Watch {
		internal static string HeaderInformation => Resources.Strings.Watch_HeaderInformation;
		internal static string StartMonitoringProject (string projectRootPath) => string.Format (Resources.Strings.Watch_StartMonitoringProject, projectRootPath);
		internal static string StopMonitoringProject (string projectRootPath) => string.Format (Resources.Strings.Watch_StopMonitoringProject, projectRootPath);
		internal static string FileChangeFilter (string filter) => string.Format (Resources.Strings.Watch_FileChangeFilter, filter);
		internal static string FileRenamed (string oldPath, string newPath, string projectName) => string.Format (Resources.Strings.Watch_FileRenameDetected, oldPath, newPath, projectName);
		internal static string FileChanged (string path, string projectName) => string.Format (Resources.Strings.Watch_FileChangeDetected, path, projectName);
		internal static string ErrorWhileMonitoring (string path) => string.Format (Resources.Strings.Watch_ErrorMonitoringProjectFiles, path);
		internal static string PausingMonitoring => Resources.Strings.Watch_PausingMonitoring;
		internal static string Syncing => Resources.Strings.Watch_Syncing;
		internal static string ResumingMonitoring => Resources.Strings.Watch_ResumingMonitoring;
		internal static string WorkerException (string messageId, string exceptionMessage) => string.Format (Resources.Strings.Watch_WorkerException, messageId, exceptionMessage);
	}

	internal static class TypeService {

		internal static string DuplicateType (string type) => string.Format (Resources.Strings.TypeService_DuplicateType, type);
		internal static string MappingMismatch (string oldClrType, string oldObjCType, string newClrType, string newObjCType) => string.Format (Resources.Strings.TypeService_MappingMismatch, oldClrType, oldObjCType, newClrType, newObjCType);
		internal static string MappingNotFound (string clrType, string objCType) => string.Format (Resources.Strings.TypeService_MappingNotFound, clrType, objCType);
		internal static string MappingUpdateFailed (string clrType, string objCType) => string.Format (Resources.Strings.TypeService_MappingUpdateFailed, clrType, objCType);
		internal static string MissingAssemblyName => Resources.Strings.TypeService_MissingAssemblyName;
		internal static string DuplicateCompilation (string assemblyName) => string.Format (Resources.Strings.TypeService_DuplicateCompilation, assemblyName);
		internal static string CompilationNotFound (string assemblyName) => string.Format (Resources.Strings.TypeService_CompilationNotFound, assemblyName);
		internal static string TypeNotFound (string typeName) => string.Format (Resources.Strings.TypeService_TypeNotFound, typeName);
		internal static string SyntaxRootNotFound (string typeName) => string.Format (Resources.Strings.TypeService_SyntaxRootNotFound, typeName);
		internal static string CompilationErrorsFound (string assemblyName) => string.Format (Resources.Strings.TypeService_CompilationErrorsFound, assemblyName);
		internal static string AssemblyDiagnosticError (string assemblyName, string diagnostic) => string.Format (Resources.Strings.TypeService_AssemblyDiagnosticError, assemblyName, diagnostic);
		internal static string AssemblyUpdateError (string assemblyName) => string.Format (Resources.Strings.TypeService_AssemblyUpdateError, assemblyName);
	}

	internal static class XcodeWorkspace {
		internal static string XcodeProjectNotFound (string path) => string.Format (Resources.Strings.XcodeWorkspace_XcodeProjectNotFound, path);
		internal static string FailToLoadXcodeProject (string path) => string.Format (Resources.Strings.XcodeWorkspace_FailToLoadXcodeProject, path);
		internal static string XcodeProjectDoesNotContainObjects (string path) => string.Format (Resources.Strings.XcodeWorkspace_XcodeProjectDoesNotContainObjects, path);
		internal static string ProcessingObjCImplementation (string objcType) => string.Format (Resources.Strings.XcodeWorkspace_ProcessingObjCImplementation, objcType);
		internal static string NoTypesFound (string objcType) => string.Format (Resources.Strings.XcodeWorkspace_NoTypesFound, objcType);
		internal static string MultipleTypesFound (string objcType) => string.Format (Resources.Strings.XcodeWorkspace_MultipleTypesFound, objcType);
		internal static string UnexpectedTypesFound (string objcType) => string.Format (Resources.Strings.XcodeWorkspace_UnexpectedTypesFound, objcType);
		internal static string ErrorUpdatingRoslynType (string roslynTypeName, string objCType, string exceptionMessage, string stackTrace) => string.Format (Resources.Strings.XcodeWorkspace_ErrorUpdatingRoslynType, roslynTypeName, objCType, exceptionMessage, stackTrace);
		internal static string ErrorParsing (string path, string translationUnitError) => string.Format (Resources.Strings.XcodeWorkspace_ErrorParsing, path, translationUnitError);
		internal static string FileParsingHasDiagnostics (string path) => string.Format (Resources.Strings.XcodeWorkspace_FileParsingHasDiagnostics, path);
		internal static string FileDiagnostics (string path) => string.Format (Resources.Strings.XcodeWorkspace_FileDiagnostics, path);
		internal static string FatalDiagnosticIssue (string diagnostic) => string.Format (Resources.Strings.XcodeWorkspace_FatalDiagnosticIssue, diagnostic);
		internal static string ErrorDiagnosticIssue (string diagnostic) => string.Format (Resources.Strings.XcodeWorkspace_ErrorDiagnosticIssue, diagnostic);
		internal static string WarningDiagnosticIssue (string diagnostic) => string.Format (Resources.Strings.XcodeWorkspace_WarningDiagnosticIssue, diagnostic);
		internal static string NoteDiagnosticIssue (string diagnostic) => string.Format (Resources.Strings.XcodeWorkspace_NoteDiagnosticIssue, diagnostic);
		internal static string SkipProcessing (string path) => string.Format (Resources.Strings.XcodeWorkspace_SkipProcessing, path);
		internal static string ProcessingFile (string path) => string.Format (Resources.Strings.XcodeWorkspace_ProcessingFile, path);
		internal static string ErrorProcessing (string path, string error, string stackTrace) => string.Format (Resources.Strings.XcodeWorkspace_ErrorProcessing, path, error, stackTrace);
	}

	internal static class SyncContext {
		internal static string SyncComplete => Resources.Strings.SyncContext_SyncComplete;
		internal static string GeneratingFiles => Resources.Strings.SyncContext_GeneratingFiles;
	}
}
