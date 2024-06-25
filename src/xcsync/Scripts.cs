// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Utils;

namespace xcsync;

static class Scripts {

	static Execution ExecuteCommand (string command, string [] args, TimeSpan timeout)
	{
		var exec = Execution.RunAsync (command, args, mergeOutput: true, timeout: timeout).Result;

		if (exec.TimedOut)
			throw new TimeoutException ($"'{command} {exec.Arguments}' execution took > {timeout.TotalSeconds} seconds, process has timed out");

		if (exec.ExitCode != 0)
			throw new InvalidOperationException ($"'{command} {exec.Arguments}' execution failed with exit code: " + exec.ExitCode);

		return exec;
	}

#pragma warning disable IO0006 // Replace Path class with IFileSystem.Path for improved testability
	public static string SelectXcode ()
	{
		var exec = ExecuteCommand ("xcode-select", ["-p"], TimeSpan.FromMinutes (1));
		return Path.GetFullPath ($"{exec.StandardOutput?.ToString ()?.Trim ('\n')}/../..");
	}
#pragma warning restore IO0006 // Replace Path class with IFileSystem.Path for improved testability

	public static List<string> GetTfms (IFileSystem fileSystem, string projPath)
	{
		var resultFile = fileSystem.Path.GetTempFileName ();
		var args = new [] { "msbuild", projPath, "-getProperty:TargetFrameworks,TargetFramework", $"-getResultOutputFile:{resultFile}" };

		ExecuteCommand ("dotnet", args, TimeSpan.FromMinutes (1));

		var jsonObject = JObject.Parse (fileSystem.File.ReadAllText (resultFile));

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

	public static string GetSupportedOSVersion (IFileSystem fileSystem, string projPath, string tfm)
	{
		var resultFile = fileSystem.Path.GetTempFileName ();
		var args = new [] { "msbuild", projPath, "-getProperty:SupportedOSPlatformVersion", $"-property:TargetFramework={tfm}", $"-getResultOutputFile:{resultFile}" };
		ExecuteCommand ("dotnet", args, TimeSpan.FromMinutes (1));

		return fileSystem.File.ReadAllText (resultFile).Trim ('\n');
	}

	public static HashSet<string> GetAssets (IFileSystem fileSystem, string projPath, string tfm)
	{
		var resultFile = fileSystem.Path.GetTempFileName ();
		//maybe add support for ImageAsset? But right now doesn't seem v necessary? (Default is BundleResource)
		var args = new [] { "msbuild", projPath, "-getItem:BundleResource", $"-property:TargetFramework={tfm}", $"-getResultOutputFile:{resultFile}" };
		ExecuteCommand ("dotnet", args, TimeSpan.FromMinutes (1));

		Console.WriteLine (fileSystem.File.ReadAllText (resultFile));
		// dynamic cuz don't wanna create a whole class to rep the incoming json
		dynamic data = JsonConvert.DeserializeObject (fileSystem.File.ReadAllText (resultFile))!;
		var bundleResources = data.Items.BundleResource;
		HashSet<string> assetPaths = new ();

		// iterate through bundle resources , specific to tfm, and compute the appropriate asset paths
		foreach (var item in bundleResources) {
			var id = item.Identity.ToString ().Replace ('\\', '/');
			if (id.Contains ("Assets.xcassets")) {
				if (!fileSystem.Path.IsPathRooted (id))
					// Combine with the project path if it's not a full path
					id = fileSystem.Path.Combine(fileSystem.Path.GetDirectoryName (projPath), id);

				// Strip off anything after ".xcassets"
				var index = id.IndexOf(".xcassets", StringComparison.Ordinal);
				if (index > -1)
					id = id.Substring(0, index + ".xcassets".Length);

				assetPaths.Add (id);
			}
		}
		return assetPaths;
	}

	public static void CopyDirectory (IFileSystem fileSystem, string sourceDir, string destinationDir, bool recursive)
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
			file.CopyTo (targetFilePath);
		}

		// If recursive and copying subdirectories, recursively call this method
		if (recursive) {
			foreach (var subDir in dirs) {
				string newDestinationDir = fileSystem.Path.Combine (destinationDir, subDir.Name);
				CopyDirectory (fileSystem, subDir.FullName, newDestinationDir, true);
			}
		}
	}

	public static string Run (string script)
	{
		var args = new [] { "-e", script };
		var exec = ExecuteCommand ("/usr/bin/osascript", args, TimeSpan.FromMinutes (1));
		return exec.StandardOutput?.ToString ()?.Trim ('\n')!;
	}

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
