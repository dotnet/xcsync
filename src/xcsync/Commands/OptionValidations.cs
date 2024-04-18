// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xamarin;

namespace xcsync.Commands;

public static class ApplePlatforms {
	public static IReadOnlyDictionary<string, (Frameworks SupportedFrameworks, string MinOsVersion, string MaxOsVersion)> platforms = new Dictionary<string, (Frameworks, string, string)>
	{
		{ "ios", (Frameworks.GetiOSFrameworks(false), SdkVersions.DotNetMiniOS, SdkVersions.iOS) },
		{ "maccatalyst", (Frameworks.GetMacCatalystFrameworks(), SdkVersions.DotNetMinMacCatalyst, SdkVersions.MacCatalyst) },
		{ "macos", (Frameworks.MacFrameworks, SdkVersions.DotNetMinOSX, SdkVersions.OSX) },
		{ "tvos", (Frameworks.TVOSFrameworks, SdkVersions.DotNetMinTVOS, SdkVersions.TVOS) },
	};
}

public delegate string? OptionValidation (string path);

public static class OptionValidations {

	public static List<string> AppleTfms = [];

	public static string? PathNameValid (string path) =>
		string.IsNullOrWhiteSpace (path)
			? Strings.Errors.Validation.PathNameEmpty
			: null;

	public static string? PathExists (string path) =>
		!Path.Exists (path)
			? Strings.Errors.Validation.PathDoesNotExist (path)
			: null;

	public static string? PathIsEmpty (string path) =>
		Directory.EnumerateFiles (path)
			.Any ()
			? Strings.Errors.Validation.PathNotEmpty (path)
			: null;

	public static string? PathCleaned (string path)
	{
		Directory.Delete (path, true);
		Directory.CreateDirectory (path);
		return null;
	}

	public static string? PathContainsValidTfm (string path)
	{
		if (!path.IsCsprojValid ())
			return Strings.Errors.Validation.PathDoesNotContainCsproj (path);

		if (!path.TryGetTfm (out var tfms))
			return Strings.Errors.Validation.MissingTfmInCsproj(path);

		AppleTfms = tfms;
		return IsTfmValid (ref tfms);
	}

	static bool TryGetTfm (this string csproj, [NotNullWhen (true)] out List<string>? tfms)
	{
		tfms = null;
		try {
			var csprojDocument = XDocument.Load (csproj);

			tfms = csprojDocument
				.Descendants ("TargetFramework")
				.FirstOrDefault ()?.Value.Split (';').ToList () ?? csprojDocument
				.Descendants ("TargetFrameworks")
				.FirstOrDefault ()?.Value.Split (';').ToList ();

			return tfms is not null;
		} catch {
			// in case there are issues when loading the file
			return false;
		}
	}

	public static bool IsCsprojValid (this string csproj) =>
		Path.GetExtension (csproj).Equals (".csproj", StringComparison.OrdinalIgnoreCase);

	public static string? IsTfmValid (ref List<string> tfms)
	{
		List<string> validTfms = new ();
		foreach (var tfm in tfms) {
			foreach (var platform in ApplePlatforms.platforms) {
				if (tfm.Contains (platform.Key)) {
					string ApplePlatform = platform.Key;

					string ValidFrameworks = $@"^net\d+\.\d+-{ApplePlatform}(?:\d+\.\d+)?$";

					if (Regex.IsMatch (tfm, ValidFrameworks))
						validTfms.Add (tfm);
				}
			}
		}

		if (validTfms.Count > 0) {
			tfms = validTfms;
			return null;
		}

		string targetFrameworks = string.Join (", ", tfms.Select (x => x.ToString ()));
		return Strings.Errors.Validation.InvalidTfmInCsproj (targetFrameworks);
	}
}
