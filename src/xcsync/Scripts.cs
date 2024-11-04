// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Utils;

namespace xcsync;

static class Scripts {

#pragma warning disable IO0006 // Replace Path class with IFileSystem.Path for improved testability
	static string PathToDotnet => Path.Combine (xcSync.DotnetPath, "dotnet");
#pragma warning restore IO0006 // Replace Path class with IFileSystem.Path for improved testability

	static Execution ExecuteCommand (string command, string [] args, TimeSpan timeout)
	{
		xcSync.Logger?.Debug ($"Executing: {command} {string.Join (' ', args)}");
		var exec = Execution.RunAsync (command, args, mergeOutput: true, timeout: timeout).Result;

		if (exec.TimedOut)
			throw new TimeoutException ($"'{command} {exec.Arguments}' execution took > {timeout.TotalSeconds} seconds, process has timed out");

		if (exec.ExitCode != 0)
			throw new InvalidOperationException ($"'{command} {exec.Arguments}' execution failed with exit code: " + exec.ExitCode);

		return exec;
	}

	public static string RunAppleScript (string script)
	{
		var args = new [] { "-e", script };
		var exec = ExecuteCommand ("/usr/bin/osascript", args, TimeSpan.FromMinutes (1));
		return exec.StandardOutput?.ToString ()?.Trim ('\n')!;
	}

	public static void CopyDirectory (IFileSystem fileSystem, string sourceDir, string destinationDir, bool recursive, bool overwrite = false)
	{
		// Get information about the source directory
		var dir = fileSystem.DirectoryInfo.New (sourceDir);
		// Check if the source directory exists
		if (!dir.Exists)
			throw new DirectoryNotFoundException ($"Source directory not found: {dir.FullName}");

		// Cache directories before we start copying
		var dirs = dir.GetDirectories ();

		// Create the destination directory
		Directory.CreateDirectory (destinationDir);

		// Get the files in the source directory and copy to the destination directory
		foreach (var file in dir.GetFiles ()) {
			string targetFilePath = fileSystem.Path.Combine (destinationDir, file.Name);
			file.CopyTo (targetFilePath, overwrite: overwrite);
		}

		// If recursive and copying subdirectories, recursively call this method
		if (recursive) {
			foreach (var subDir in dirs) {
				string newDestinationDir = fileSystem.Path.Combine (destinationDir, subDir.Name);
				CopyDirectory (fileSystem, subDir.FullName, newDestinationDir, recursive: recursive, overwrite: overwrite);
			}
		}
	}

#pragma warning disable IO0002 // Replace File class with IFileSystem.File for improved testability
#pragma warning disable IO0006 // Replace Path class with IFileSystem.Path for improved testability

	public static bool ConvertPbxProjToJson (string projectPath)
	{
		var exec = ExecuteCommand ("plutil", ["-convert", "json", projectPath], TimeSpan.FromMinutes (1));
		return File.Exists (projectPath) && exec.ExitCode == 0;
	}

	public static string GetSupportedOSVersionForTfmFromProject (string projPath, string tfm)
	{
		var resultFile = Path.GetTempFileName ();
		var args = new [] { "msbuild", projPath, "-getProperty:SupportedOSPlatformVersion", $"-property:TargetFramework={tfm}", $"-getResultOutputFile:{resultFile}" };
		ExecuteCommand (PathToDotnet, args, TimeSpan.FromMinutes (1));

		return File.ReadAllText (resultFile).Trim ('\n');
	}

	public static List<string> GetTargetFrameworksFromProject (string projPath)
	{
		var resultFile = Path.GetTempFileName ();
		var args = new [] { "msbuild", projPath, "-getProperty:TargetFrameworks,TargetFramework", $"-getResultOutputFile:{resultFile}" };

		ExecuteCommand (PathToDotnet, args, TimeSpan.FromMinutes (1));

		var jsonObject = JObject.Parse (File.ReadAllText (resultFile));

		List<string> tfms = [];

		var tfmsToken = jsonObject.SelectToken ("$.Properties.TargetFrameworks");

		if (tfmsToken is not null)
			tfms = tfmsToken
				.ToString ()
				.Split (';')
				.Where (tfm => !string.IsNullOrWhiteSpace (tfm))
				.ToList ();

		var tfmToken = jsonObject.SelectToken ("$.Properties.TargetFramework")?.ToString ();

		if (!string.IsNullOrEmpty (tfmToken))
			// to deal w nullability
			tfms = [.. tfms, tfmToken];

		return tfms;
	}

	public static HashSet<string> GetAssetItemsFromProject (string projPath, string tfm)
	{
		var resultFile = Path.GetTempFileName ();
		//maybe add support for ImageAsset? But right now doesn't seem v necessary? (Default is BundleResource)
		var args = new [] { "msbuild", projPath, "-getItem:BundleResource", $"-property:TargetFramework={tfm}", $"-getResultOutputFile:{resultFile}" };
		ExecuteCommand (PathToDotnet, args, TimeSpan.FromMinutes (1));

		// dynamic cuz don't wanna create a whole class to rep the incoming json
		dynamic data = JsonConvert.DeserializeObject (File.ReadAllText (resultFile))!;
		var bundleResources = data.Items.BundleResource;
		HashSet<string> assetPaths = new ();

		// iterate through bundle resources , specific to tfm, and compute the appropriate asset paths
		foreach (var item in bundleResources) {
			var id = item.Identity.ToString ().Replace ('\\', '/');
			if (id.Contains ("Assets.xcassets")) {
				if (!Path.IsPathRooted (id))
					// Combine with the project path if it's not a full path
					id = Path.Combine (Path.GetDirectoryName (projPath), id);

				// Strip off anything after ".xcassets"
				var index = id.IndexOf (".xcassets", StringComparison.Ordinal);
				if (index > -1)
					id = id.Substring (0, index + ".xcassets".Length);

				assetPaths.Add (id);
			}
		}
		return assetPaths;
	}

	public static List<string> GetFileItemsFromProject (string projPath, string tfm, string targetPlatform)
	{
		var resultFile = Path.GetTempFileName ();
		var args = new [] { "msbuild", projPath, "-getItem:Compile,None", $"-property:TargetFramework={tfm}", $"-getResultOutputFile:{resultFile}" };
		ExecuteCommand (PathToDotnet, args, TimeSpan.FromMinutes (1));

		var jsonObject = JObject.Parse (File.ReadAllText (resultFile));

		// Combine the search for Compile and None tokens into one operation
		var tokens = jsonObject.SelectTokens ("$..['Compile','None'][*].FullPath");

		return jsonObject.SelectTokens ("$..['Compile','None'][*].FullPath")
		   .Select (token => token.ToString ())
		   .Where (path => !path.Contains ("Platforms") || path.Contains ($"Platforms/{targetPlatform}", StringComparison.OrdinalIgnoreCase))
		   .Distinct ()
		   .ToList ();
	}

	public static bool IsMauiAppProject (string projPath)
	{
		var resultFile = Path.GetTempFileName ();
		var args = new [] { "msbuild", projPath, "-getProperty:UseMaui,OutputType", $"-getResultOutputFile:{resultFile}" };
		ExecuteCommand (PathToDotnet, args, TimeSpan.FromMinutes (1));

		var jsonObject = JObject.Parse (File.ReadAllText (resultFile));
		var useMaui = string.CompareOrdinal (jsonObject.SelectToken ("$.Properties.UseMaui")?.ToString ().ToLowerInvariant (), "true") == 0;
		var outputType = jsonObject.SelectToken ("$.Properties.OutputType")?.ToString ();

		return useMaui && string.CompareOrdinal (outputType, "Exe") == 0;
	}

	public static string SelectXcode ()
	{
		var exec = ExecuteCommand ("xcode-select", ["-p"], TimeSpan.FromMinutes (1));
		return Path.GetFullPath ($"{exec.StandardOutput?.ToString ()?.Trim ('\n')}/../..");
	}

#pragma warning restore IO0006 // Replace Path class with IFileSystem.Path for improved testability
#pragma warning restore IO0002 // Replace File class with IFileSystem.File for improved testability

	public static string OpenXcodeProject (string workspacePath) =>
		$@"
			set workspacePath to ""{workspacePath}""
			tell application ""{SelectXcode ()}""
				activate
				open workspacePath
			end tell";

	public static string CheckXcodeProject (string projectPath) =>
		$@"
			tell application ""{SelectXcode ()}""
				with timeout of 60 seconds
					set projectPath to ""{projectPath}""
					repeat with doc in workspace documents
						if path of doc is projectPath then
							return true
							exit repeat
						end if
					end repeat
				end timeout
				return false
			end tell";

	public static string CloseXcodeProject (string projectPath) =>
		$@"
			tell application ""{SelectXcode ()}""
				set projectPath to ""{projectPath}""
				with timeout of 60 seconds
					repeat with doc in documents
						if path of doc is projectPath then
							close doc
							return true
						end if
					end repeat
				end timeout
				return false
				end tell";
}
