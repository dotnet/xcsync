
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
			internal static string PathNotEmpty (string path) => string.Format (Resources.Strings.Errors_Validation_PathIsNotEmpty, path);
			internal static string PathDoesNotContainCsproj (string path) => string.Format (Resources.Strings.Errors_Validation_PathDoesNotContainCsproj, path);
			internal static string MissingTfmInCsproj (string path) => string.Format (Resources.Strings.Errors_Validation_MissingValidTargetFrameworkInPath, path);
			internal static string InvalidTfmInCsproj (string tfm) => string.Format (Resources.Strings.Errors_Validation_InvalidTargetFrameworkInCsproj, tfm);
		}

		internal static string InvalidOption (string option, string error) => string.Format (Resources.Strings.Errors_InvalidOption, option, error);
		internal static string MultipleProjectFilesFound => Resources.Strings.Errors_MultipleProjectFilesFound;
		internal static string CsprojNotFound => Resources.Strings.Errors_CsprojNotFound;
		internal static string MultipleTfmsFound => Resources.Strings.Errors_MultipleTfmsFound;
		internal static string TargetPlatformNotFound => Resources.Strings.Errors_TargetPlatformNotFound;
		internal static string TfmNotSupported => Resources.Strings.Errors_TfmNotSupported;
	}

	internal static class Generate {
		internal static string HeaderInformation => Resources.Strings.Generate_HeaderInformation;

	}

	internal static class Sync {
		internal static string HeaderInformation => Resources.Strings.Sync_HeaderInformation;
	}
	internal static class Watch {
		internal static string HeaderInformation => Resources.Strings.Watch_HeaderInformation;
	}

}