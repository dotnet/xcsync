// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace xcsync;

static class FileSystem {
	public static string Combine (params string [] paths)
	{
		return Path.Combine (paths);
	}

	public static bool FileExists (string file)
	{
		return File.Exists (file);
	}

	public static bool DirectoryExists (string directory)
	{
		return Directory.Exists (directory);
	}

	public static void CreateDirectory (string path)
	{
		Directory.CreateDirectory (path);
	}

	public static void Delete (string directory, bool recursive)
	{
		Directory.Delete (directory, recursive);
	}

	public static string GetExtension (string path)
	{
		return Path.GetExtension (path);
	}

	public static IEnumerable<string> EnumerateFiles (string directory, string pattern, SearchOption options)
	{
		return Directory.EnumerateFiles (directory, pattern, options);
	}

	public static string? GetDirectoryName (string projectPath)
	{
		return Path.GetDirectoryName (projectPath);
	}

	internal static IEnumerable<string> EnumerateFileSystemEntries (string targetPath)
	{
		return Directory.EnumerateFileSystemEntries (targetPath);
	}

}
