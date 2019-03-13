#tool "GitVersion.CommandLine"
#tool "ReportUnit"
#tool "nuget:?package=NunitXml.TestLogger"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target                  = Argument("target", "Default");
var configuration           = Argument("configuration", "Release");
var solutionPath            = MakeAbsolute(File(Argument("solutionPath", "./Storm.GoogleAnalytics.sln")));
var solutionFolder          = MakeAbsolute(Directory(Argument("solutionPath", "./")));

var testProjects            = Enumerable.Empty<string>();

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

var testAssemblyBinFormat   = "./tests/{0}/bin/" +configuration +"/{0}.dll";

var artifacts               = MakeAbsolute(Directory(Argument("artifactPath", "./artifacts")));
var buildOutput             = MakeAbsolute(Directory(artifacts +"/build/"));

var testResults     	    = MakeAbsolute(Directory(artifacts + "/test-results"));
var testLog			        = File(testResults + "/TestLog.log");
var testReport              = File(testResults + "/TestReport.html");

GitVersion versionInfo      = null;
var versionAssemblyInfo     = MakeAbsolute(File(Argument("versionAssemblyInfo", "VersionAssemblyInfo.cs")));

SolutionParserResult solution               = null;
DirectoryPath projectDirectory              = null;
FilePath projectPath                        = null;

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Setup(ctx => {
    if(!FileExists(solutionPath)) {
        throw new Exception(string.Format("Solution file not found - {0}", solutionPath.ToString()));
    }
    solution = ParseSolution(solutionPath.ToString());

    Information("[Setup] Using Solution '{0}'", solutionPath.ToString());

    var project = solution.Projects.FirstOrDefault();
    if(project == null) {
        throw new Exception(string.Format("Unable to find any projects in solution '{0}'", solutionPath.GetFilenameWithoutExtension()));
    }
    projectPath = project.Path;
    projectDirectory = System.IO.Path.GetDirectoryName(projectPath.ToString());
});

Task("Clean")
    .Does(() =>
{
    CleanDirectories(artifacts.ToString());
    CreateDirectory(artifacts);
    CreateDirectory(buildOutput);
    
    var binDirs = GetDirectories(solutionPath.GetDirectory() +@"\src\**\bin");
    var objDirs = GetDirectories(solutionPath.GetDirectory() +@"\src\**\obj");
    CleanDirectories(binDirs);
    CleanDirectories(objDirs);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore(new DotNetCoreRestoreSettings {
        WorkingDirectory = solutionFolder
    });
});

Task("Update-Version-Info")
    .IsDependentOn("CreateVersionAssemblyInfo")
    .Does(() => 
{
        versionInfo = GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = true,
            UpdateAssemblyInfoFilePath = versionAssemblyInfo
        });

    if(versionInfo != null) {
        Information("Version: {0}", versionInfo.FullSemVer);
    } else {
        throw new Exception("Unable to determine version");
    }
});

Task("CreateVersionAssemblyInfo")
    .WithCriteria(() => !FileExists(versionAssemblyInfo))
    .Does(() =>
{
    Information("Creating version assembly info");
    CreateAssemblyInfo(versionAssemblyInfo, new AssemblyInfoSettings {
        Version = "0.0.0.0",
        FileVersion = "0.0.0.0",
        InformationalVersion = "",
    });
});

Task("Update-AppVeyor-Build-Number")
    .IsDependentOn("Update-Version-Info")
    .WithCriteria(() => AppVeyor.IsRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UpdateBuildVersion(versionInfo.FullSemVer +" | " +AppVeyor.Environment.Build.Number);
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Update-Version-Info")
    .Does(() =>
{
    DotNetCorePublish(projectPath.ToString(), new DotNetCorePublishSettings {
        OutputDirectory = Directory(artifacts + "/build/"),
        Configuration = configuration,
        NoRestore = true
    });
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .WithCriteria(() => testProjects.Any())
    .Does(() => 
{
    var testSettings = new DotNetCoreTestSettings {
        ResultsDirectory = testResults,
        Configuration = configuration,
        Logger = "nunit",
        TestAdapterPath = Directory("./"),
        DiagnosticFile = testLog
    };

    foreach(var assembly in testProjects) {
        DotNetCoreTest(assembly, testSettings);
    }
}).Finally(() => {
    ReportUnit(testResults + "/TestResults.xml", testReport);
    if(AppVeyor.IsRunningOnAppVeyor) {
        AppVeyor.UploadTestResults(testResults + "/TestResults.xml", AppVeyorTestResultsType.XUnit);
        AppVeyor.UploadArtifact(testLog);
        AppVeyor.UploadArtifact(testReport);
    }
});

Task("Create-NuGet-Packages")
    .IsDependentOn("Build")
    .Does(() => {
        var outputDirectory = artifacts + "/packages";
        EnsureDirectoryExists(outputDirectory);

        var settings = new DotNetCorePackSettings {
            ArgumentCustomization = args => args.Append($"-p:Version={versionInfo.NuGetVersion}"),
            Configuration = configuration,
            OutputDirectory = outputDirectory,
        };

        DotNetCorePack(projectPath.ToString(), settings);
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Update-Version-Info")
    .IsDependentOn("Update-AppVeyor-Build-Number")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Package")
    ;


Task("Package")
    .IsDependentOn("Build")
    .IsDependentOn("Create-NuGet-Packages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
