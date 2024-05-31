// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO.Abstractions;
using Newtonsoft.Json.Linq;
using Xamarin.Utils;

namespace xcsync;

static partial class Scripts {

	static Execution ExecuteCommand (string command, string [] args, TimeSpan timeout)
	{
		var exec = Execution.RunAsync (command, args, mergeOutput: true, timeout: timeout).Result;

		if (exec.TimedOut)
			throw new TimeoutException ($"'{command} {exec.Arguments}' execution took > {timeout.TotalSeconds} seconds, process has timed out");

		if (exec.ExitCode != 0)
			throw new InvalidOperationException ($"'{command} {exec.Arguments}' execution failed with exit code: " + exec.ExitCode);

		return exec;
	}

	static string SelectXcode ()
	{
		var exec = ExecuteCommand ("xcode-select", new [] { "-p" }, TimeSpan.FromMinutes (1));
		return $"{exec.StandardOutput?.ToString ()?.Trim ('\n')}/../..";
	}

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
		var args = new [] { "msbuild", projPath, "-getProperty:SupportedOSPlatformVersion", $"-property:TargetFramework={tfm}", $"-getResultOutputFile:{resultFile}"};
		var exec = Execution.RunAsync ("dotnet", args, mergeOutput: true, timeout: TimeSpan.FromMinutes (1)).Result;

		if (exec.TimedOut)
			throw new TimeoutException ($"'dotnet {exec.Arguments}' execution took > 60 seconds, process has timed out");

		if (exec.ExitCode != 0)
			throw new InvalidOperationException ($"'dotnet {exec.Arguments}' execution failed with exit code: " + exec.ExitCode);

		return fileSystem.File.ReadAllText (resultFile).Trim ('\n');
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
