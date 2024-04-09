using xcsync.Projects;

namespace xcsync.tests;

public class Base {

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
}
