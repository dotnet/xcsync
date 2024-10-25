# Set up your machine to use the latest `xcsync` builds

These instructions will get you set up with the latest build of `xcsync` from the main branch. If you just want the lastest preview release of .NET xcsync, see the [README.md](../README.md#installation-and-usage).  


## Install the tool

The latest builds are pushed to a special NuGet feed, which you need specify when installing the tool:
```sh
dotnet tool install dotnet-xcsync -g --prerelease --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet9/nuget/v3/index.json
```

## Usage
The following section describes how to use the `xcsync` tool to generate and sync an Xcode project with a .NET project. To start you will need to have either an existing .NET project or create a new one. Use the [Create a new .NET project](#create-a-new-net-project) section to create a new project, otherwise jump to the [Generate an Xcode Project from a .NET project](#generate-an-xcode-project-from-a-net-project).

# Create a new .NET project

Create an empty .NET MAUI project on the command line using one of the `maui`, `ios`, `tvos`, `maccatalyst`, or `macos` templates:
```shell
dotnet new {template}
```
Replacing the `{template}` placeholder with the template name.

The resulting project can be built using the following commands:
```shell
dotnet restore
dotnet build
```

# Generate an Xcode Project from a .NET project
Make sure your current directory contains your .NET project then run the `xcsync` `generate` command:
```shell
xcsync generate 
```

By default the generated project will be located in the `obj/xcode` folder of the project.

If you are testing on macOS then you can add the optional `--open` argument to open the generated project in Xcode.

## Sync changes from the Xcode project
First follow the steps to generate the Xcode project above, then run the `xcsync` `sync` command:
```shell
xcsync sync 
```

This will synchronize any changes made to the Xcode project back into the .NET project.
