using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace xcsync.Commands;

public delegate string? OptionValidation (DirectoryInfo path);

public static class OptionValidations {
	public static string? PathNameValid (DirectoryInfo path) =>
		string.IsNullOrWhiteSpace (path.Name)
			? "Path cannot be empty"
			: null;

	public static string? PathExists (DirectoryInfo path) =>
		!path.Exists
			? "Path does not exist"
			: null;

	public static string? PathIsEmpty (DirectoryInfo path) =>
		path.EnumerateFiles ()
			.Any ()
			? "Path is not empty"
			: null;

	public static string? PathCleaned (DirectoryInfo path)
	{
		path.Delete (true);
		path.Create ();
		return null;
	}

	private static readonly string ValidFrameworks =
		@"^net[7-9]\.\d+-(macos|ios|maccatalyst|tvos)$";

	public static string? PathContainsValidTfm (DirectoryInfo path)
	{
		if (!TryGetCsProj (path, out string? csproj))
			return "Path does not contain a C# project";

		if (!TryGetTfm (csproj, out string? tfm))
			return "Missing target framework in csproj";

		return IsTfmValid (tfm);
	}

	private static bool TryGetCsProj (DirectoryInfo path, [NotNullWhen (true)] out string? csproj)
	{
		csproj = path.EnumerateFiles ("*.csproj").FirstOrDefault ()?.FullName;
		return csproj is not null;
	}

	private static bool TryGetTfm (string csproj, [NotNullWhen (true)] out string? tfm)
	{
		try {
			XDocument csprojDocument = XDocument.Load (csproj);
			tfm = csprojDocument.Descendants ("TargetFramework").FirstOrDefault ()?.Value;
			return tfm is not null;
		} catch {
			// in case there are issues when loading the file
			tfm = null;
			return false;
		}

	}

	public static string? IsTfmValid (string tfm) =>
		Regex.IsMatch (tfm, ValidFrameworks)
			? null
			: "Invalid target framework in csproj";
}
