# Set up your machine to use the latest Xcsync builds

These instructions will get you set up with the latest build of Xcsync. If you just want the last preview release of .NET Xcsync, the packages are on nuget.org.

## Prepare the machine

See [machine-requirements.md](machine-requirements.md).

## Add necessary NuGet feeds

The latest builds are pushed to a special feed, which you need to add:
```sh
dotnet nuget add source --name dotnet8 https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet8/nuget/v3/index.json
```

## Generate the Xcode project

Create an empty .NET MAUI project on the command line using one of the maui, ios, tvos, maccatalyst, or macos templates:
```shell
dotnet new {template}
```

These will create a `.sln` file and a project.

Assuming the NuGet feed you added above is visible -- for example you added it globally or it's in a NuGet.config in this folder - you can now build that `.sln`
```shell
dotnet restore
dotnet build
```

And then run the xcsync generate command:
```shell
dotnet xcsync generate 
```

By default the generated project will be located in the `obj/xcode` folder of the project.

If you are testing on macOS then you can add the optional `--open` argument to open the generated project in Xcode.

## Sync changes from the Xcode project
First follow the steps to generate the Xcode project above, then run the xcsync sync command:
```shell
dotnet xcsync sync 
```

This will synchronize any changes made to the Xcode project back into the .NET project.

