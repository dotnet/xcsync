
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
	}

	internal static class TypeService {
		internal static string DuplicateType (string type) => string.Format (Resources.Strings.TypeService_DuplicateType, type);
	}
}
