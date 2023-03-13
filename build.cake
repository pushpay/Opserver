var target = Argument("target", "Pack");
var configuration = Argument("configuration", "Release");
var version = EnvironmentVariable<string>("VERSION", "1.1.0.0");
var octopusApiKey = EnvironmentVariable<string>("OCTOPUS_API_KEY", "none");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectory($"./Opserver/bin/{configuration}");
        CleanDirectory($"./Opserver/obj/{configuration}");
        CleanDirectory($"./Opserver.Core/bin/{configuration}");
        CleanDirectory($"./Opserver.Core/obj/{configuration}");
        DeleteFiles("**/*.nupkg");
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        var solutions = GetFiles("./Opserver.sln");

        foreach(var solution in solutions)
        {
            Information("Restoring {0}", solution);
            NuGetRestore(solution);
        }
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        MSBuild("./Opserver.sln", new MSBuildSettings
        {
            Configuration = configuration,
            InformationalVersion = version,
            FileVersion = version,
            Version = version
        });
    });

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var nuGetPackSettings   = new NuGetPackSettings {
                                    RequireLicenseAcceptance = false,
                                    Symbols                  = false,
                                    Version                  = version,
                                    OutputDirectory          = "./output"
                                 };

        NuGetPack("./Opserver/Opserver.nuspec", nuGetPackSettings);
    });

Task("Push")
    .Does(() =>
    {
        var packages = GetFiles("./**/StackExchange.Opserver.*.nupkg");

        NuGetPush(packages, new NuGetPushSettings {
            ApiKey = octopusApiKey,
            Source = "https://octopus.rktapps.com/nuget/packages"
        });
    });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
