// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class SolutionPathFinder {
	public static string GetProjectRoot () => Path.GetDirectoryName (GetSolutionPath ())!;

	public static string GetSolutionPath ()
    {
        var assemblyDirectory = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location)!;
        // Navigate up directories until .sln file is found
        return FindSolutionPath (assemblyDirectory);
	}

    public static string FindSolutionPath (string startDirectory)
    {
        // Check if root of filesystem has been reached
        if (string.IsNullOrEmpty (startDirectory) || Directory.GetParent (startDirectory) == null)
            throw new FileNotFoundException ("Solution file (.sln) not found.");

        // Check if a .sln file exists in the current directory
        var solutionFiles = Directory.GetFiles (startDirectory, "*.sln", SearchOption.TopDirectoryOnly);
        
		return solutionFiles.Length > 0 
		? solutionFiles.First () 
		: FindSolutionPath (Directory.GetParent (startDirectory)!.FullName);
	}
}
