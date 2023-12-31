using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.ChangeLog;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using ParameterAttribute = Nuke.Common.ParameterAttribute;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    [Parameter("Nuspec Filename")] readonly string NuspecFile = "deploy.nuspec"; //default
    [Parameter("App build version")] readonly string BuildVersion; //Must specify
    [Parameter("Project Description")] readonly string ProjectDescription = "RabbitMQTester Nuget Package built on " + DateTime.UtcNow.ToString("MM-dd-yyyy"); //default
    [Parameter("Project Author")] readonly string ProjectAuthor = "Michael Lucas (mike@lucasnet.org)"; //default
    [Parameter("Project Copyright")] readonly string ProjectCopyright = "Copyright 2023"; //default
    [Parameter("Project VCS Url")] readonly string ProjectUrl = "https://github.com/mtlucas/RabbitMQTester"; //default
    [Parameter("NuGet repository server Url")] readonly string NugetApiUrl = "https://nuget.lucasnet.int/"; //default
    [Parameter("Nuget repository server ApiKey")] readonly string NugetApiKey;
    [Parameter("Publishes .NET runtime with app")] readonly Boolean SelfContained = true;

    //readonly private Dictionary<string, object> NuspecFiles = new () { { "NuspecFile", "deploy.nuspec" }, };

    [Solution] readonly Solution Solution;
    AbsolutePath SourceDirectory => RootDirectory / "";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTasks.DotNetPublish(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(BuildVersion)
                .SetFileVersion(BuildVersion)
                .SetVersion(BuildVersion)
                .SetInformationalVersion(BuildVersion)
                .SetCopyright(ProjectCopyright)
                .SetSelfContained(SelfContained)
                .SetRuntime(SelfContained ? "win-x64" : null)
                .SetOutput(ArtifactsDirectory / "output"));
        });

    Target Pack => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            NuGetTasks.NuGetPack(s => s
                .SetConfiguration(Configuration)
                .SetTargetPath(RootDirectory / NuspecFile)
                .SetIncludeReferencedProjects(true)
                .SetVersion(BuildVersion)
                .SetProperty("description", ProjectDescription)
                .SetProperty("copyright", ProjectCopyright)
                .SetProperty("authors", ProjectAuthor)
                .SetProperty("projectUrl", ProjectUrl)
                .SetOutputDirectory(ArtifactsDirectory / "nuget"));
        });

    [Obsolete]
    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => NugetApiUrl)
        .Requires(() => NugetApiKey)
        //.Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {

            GlobFiles(ArtifactsDirectory / "nuget" / "*.nupkg")
                .NotEmpty()
                .Where(x => !x.EndsWith("symbols.nupkg"))
                .ForEach(x =>
                {
                    NuGetTasks.NuGetPush(s => s
                        .SetTargetPath(x)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NugetApiKey)
                    );
                });
        });

    /*
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
        });
    */
}
