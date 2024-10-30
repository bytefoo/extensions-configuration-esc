using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "ubuntu-latest",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    //OnPushBranchesIgnore = new[] {"main"},
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[] {nameof(Push)},
    EnableGitHubToken = true,
    PublishArtifacts = false,
    ImportSecrets = new[] { "NUGET_PUBLISH_KEY" }
    )]
[DotNetVerbosityMapping]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitRepository] readonly GitRepository GitRepository;
    [NerdbankGitVersioning] readonly NerdbankGitVersioning NerdbankGitVersioning;

    [Solution] readonly Solution Solution;

    [Parameter(Name = "NUGET_PUBLISH_KEY")] [Secret] string NuGetApiKey;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.DeleteDirectory();
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());

            var frameworks = from project in Solution.Projects
                from framework in project.GetTargetFrameworks()
                where project.Name == "ByteFoo.Extensions.Configuration.Esc"
                select (project,framework);

            DotNetPublish(_ => _
                .SetConfiguration(Configuration)
                .SetRepositoryUrl(GitRepository.HttpsUrl)
                .SetNoRestore(SucceededTargets.Contains(Restore))
                .SetAssemblyVersion(NerdbankGitVersioning.AssemblyVersion)
                .SetFileVersion(NerdbankGitVersioning.AssemblyFileVersion)
                .SetInformationalVersion(NerdbankGitVersioning.AssemblyInformationalVersion)
                .EnableNoBuild()
                .EnableNoLogo()
                .When(IsServerBuild, _ => _
                    .EnableContinuousIntegrationBuild())
                .CombineWith(frameworks, (_, v) => _
                    .SetOutput(ArtifactsDirectory / v.framework)
                    .SetFramework(v.framework))
            );
        });

    [Parameter]
    string NuGetSource => "https://api.nuget.org/v3/index.json";

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Executes(() =>
        {
            DotNetPack(_ => _
                .SetConfiguration(Configuration)
                .SetNoBuild(SucceededTargets.Contains(Compile))
                .SetOutputDirectory(PackagesDirectory)
                .SetRepositoryUrl(GitRepository.HttpsUrl)
                .SetVersion(NerdbankGitVersioning.NuGetPackageVersion)
            );

            ReportSummary(_ => _
                .AddPair("Packages", PackagesDirectory.GlobFiles("*.nupkg").Count.ToString()));
        });

    Configure<DotNetNuGetPushSettings> PackagePushSettings => _ => _;

    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => NuGetApiKey)
        .Executes(() =>
        {
            DotNetNuGetPush(_ => _
                    .SetSource(NuGetSource)
                    .SetApiKey(NuGetApiKey)
                    .CombineWith(PackagesDirectory.GlobFiles("*.nupkg"), (_, v) => _
                        .SetTargetPath(v))
                    .Apply(PackagePushSettings),
                5,
                true);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    AbsolutePath SourceDirectory => RootDirectory / "src";

    AbsolutePath TestsDirectory => RootDirectory / "tests";

    public static int Main() => Execute<Build>(x => x.Compile);
}