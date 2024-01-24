namespace xcsync.Commands;

public class GenerateCommand {
	public static void Execute (DirectoryInfo project, DirectoryInfo target, bool force, bool open)
	{
		Console.WriteLine ($"Generating files from project '{project}' to target '{target}'");
		// Implement logic here
	}
}
