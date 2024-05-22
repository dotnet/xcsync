// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Newtonsoft.Json.Linq;
using Xamarin.Utils;

namespace xcsync;

static partial class Scripts {

	static string SelectXcode ()
	{
		var exec = Execution.RunAsync ("xcode-select", new List<string> { "-p" }, mergeOutput: true, timeout: TimeSpan.FromMinutes (1)).Result;

		if (exec.TimedOut)
			throw new TimeoutException ("'xcode-select -p' took > 60 seconds, process has timed out");

		if (exec.ExitCode != 0)
			throw new InvalidOperationException ($"'xcode-select -p' failed with exit code '{exec.ExitCode}'");

		return $"{exec.StandardOutput?.ToString ()?.Trim ('\n')}/../..";
	}

	public static List<string> GetTfms (IFileSystem fileSystem, string projPath)
	{
		var resultFile = fileSystem.Path.GetTempFileName ();
		var args = new [] { "msbuild", projPath, "-getProperty:TargetFrameworks,TargetFramework", $"-getResultOutputFile:{resultFile}" };
		var exec = Execution.RunAsync ("dotnet", args, mergeOutput: true, timeout: TimeSpan.FromMinutes (1)).Result;

		if (exec.TimedOut)
			throw new TimeoutException ($"'dotnet {exec.Arguments}' execution took > 60 seconds, process has timed out");

		if (exec.ExitCode != 0)
			throw new InvalidOperationException ($"'dotnet {exec.Arguments}' execution failed with exit code: " + exec.ExitCode);

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

	public static string Run (string script)
	{
		var args = new [] { "-e", script };
		var exec = Execution.RunAsync ("/usr/bin/osascript", args, mergeOutput: true, timeout: TimeSpan.FromMinutes (1)).Result;

		if (exec.TimedOut)
			throw new TimeoutException ($"AppleScript 'osascript {exec.Arguments}' execution took > 60 seconds, process has timed out");

		if (exec.ExitCode != 0)
			throw new InvalidOperationException ($"AppleScript: 'osascript {exec.Arguments}' execution failed with exit code: " + exec.ExitCode);

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
