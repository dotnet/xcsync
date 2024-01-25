using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace xcsync.Commands;

public delegate string? OptionValidation (string path);

public static class OptionValidations {
	public static string? PathNameValid (string path) =>
		string.IsNullOrWhiteSpace (path)
			? "Path name is empty"
			: null;

	public static string? PathExists (string path) =>
		!Path.Exists (path)
			? $"Path '{path}' does not exist"
			: null;

	public static string? PathIsEmpty (string path) =>
		Directory.EnumerateFiles (path)
			.Any ()
			? $"Path '{path}' is not empty"
			: null;

	public static string? PathCleaned (string path)
	{
		Directory.Delete (path, true);
		Directory.CreateDirectory (path);
		return null;
	}

	private static readonly string ValidFrameworks =
		@"^net[7-9]\.\d+-(macos|ios|maccatalyst|tvos)$";

	public static string? PathContainsValidTfm (string path)
	{
		if (!path.IsCsprojValid ())
			return $"Path '{path}' does not contain a C# project";

		if (!TryGetTfm (path, out string? tfm))
			return $"Missing target framework in '{path}'";

		return tfm.IsValid ();
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

	public static bool IsCsprojValid (this string csproj) =>
		Path.GetExtension (csproj).Equals (".csproj", StringComparison.OrdinalIgnoreCase);

	public static string? IsValid (this string tfm) =>
		Regex.IsMatch (tfm, ValidFrameworks)
			? null
			: $"Invalid target framework '{tfm}' in csproj";
}
