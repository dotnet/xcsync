namespace xcsync.Commands;

public class WatchCommand {
	public static void Execute (DirectoryInfo project, DirectoryInfo target, bool force)
	{
		Console.WriteLine ($"Watching files from project '{project}' to target '{target}'");
		// Implement logic here
	}
}
