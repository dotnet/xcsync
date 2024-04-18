using System.Diagnostics;
using xcsync.Projects;

namespace xcsync.tests;

public class Base {

	protected readonly string XcsyncExe =
		Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..", "..", "xcsync", "bin", "Debug", "net8.0", "xcsync");

	protected readonly string TestProjectPath =
		Path.Combine ("..", "..", "..", "..", "test-project", "test-project.csproj");

	protected readonly Dotnet DotnetProject;

	protected readonly NSProject NsProject;

	protected Base ()
	{
		if (!File.Exists (TestProjectPath))
			throw new FileNotFoundException ($"Test project not found at '{TestProjectPath}'");

		DotnetProject = new Dotnet (TestProjectPath);
		NsProject = new NSProject (DotnetProject, "macos");
	}

	protected void DotnetNew (string platform, string path)
	{
		var process = new Process ();
		process.StartInfo.FileName = "dotnet";
		process.StartInfo.Arguments = $"new {platform} -o \"{path}\"";
		process.StartInfo.RedirectStandardOutput = false;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.Start ();
		process.WaitForExit ();
	}

	protected void Xcsync (string arguments)
	{
		var process = new Process ();
		process.StartInfo.FileName = XcsyncExe;
		process.StartInfo.Arguments = arguments;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;
		process.Start ();
		process.WaitForExit ();
	}
}
