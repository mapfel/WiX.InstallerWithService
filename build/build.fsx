#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MsBuild
nuget Fake.Core.Target
nuget Fake.Installer.Wix //"

#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open System.IO
open Fake.Installer

Target.create "Nothing" ignore

Target.create "Clean" (fun _ ->
    !! "./../src/**/bin"
    ++ "./../src/**/obj"
    ++ "./../temp/"
    |> Shell.cleanDirs
)

Target.create "Build-Service" (fun _ ->
    [
      "./../src/DemoService/DemoService.csproj";
    ]
    |> Seq.iter (Fake.DotNet.MSBuild.build id)
)

Target.create "BuildWiXSetup" (fun _ ->
    // Setup some constants for later usage
    let buildDir = "./../src/DemoService/bin/Debug"
    let workDir = "./../temp/"
    let templateFile = "SetupTemplate.wxs"
    let templateFileFullPath = workDir + templateFile
    let wixToolsPath = "./../packages/WiX/tools"
    let wixCollectDir = "./../temp/"

    let setupFileName = "./../deploy/Setup.msi"
    let WixProductUpgradeGuid = "CCE3561F-0D8E-46B5-88C4-C9614D0506A1"
    
    // Ensure existing of deploy preparation folder with its sub folders for features/components
    Directory.ensure(wixCollectDir) |> ignore // works recursively

    // Collect the files and fill folder structure
    Shell.copyRecursiveTo true wixCollectDir buildDir |> ignore

    // Package Admin-Tools
    let fileFilter = fun (file : FileInfo) -> // filter for files to be processed when running bulk component creation
        file.Extension = ".exe" ||
        file.Extension = ".config"

    // filter for folders to be processed when running bulk component creation
    let dirFilter = fun (dir : DirectoryInfo) -> true

    // Collect Files which should be shipped.
    let components = Wix.bulkComponentCreation fileFilter (DirectoryInfo wixCollectDir) Wix.Architecture.X86

    let serviceInstall = Wix.generateServiceInstall (fun f -> 
                                                       {f with
                                                           Id = "Id_ServiceInstall"
                                                           Account = "[SVC_USER_ACCOUNT]"
                                                           Password = "[SVC_USER_PASSWORD]"
                                                           Description = "The description of the Service (see services.msc)"
                                                           DisplayName = "The name of the Service (see services.msc)"
                                                           Interactive = Some(Wix.YesOrNo.No)
                                                           Name = "NameOfTheService"
                                                           Start = Wix.ServiceInstallStart.Auto
                                                           Type = Wix.ServiceInstallType.OwnProcess
                                                           Vital = Wix.YesOrNo.No
                                                       })

    let serviceControl = Wix.generateServiceControl (fun f -> 
                                                       {f with 
                                                           Id = "Id_ServiceControl"
                                                           Name = "NameOfTheService"
                                                           Start = Wix.InstallUninstall.Install
                                                           Stop = Wix.InstallUninstall.Both
                                                           Remove = Wix.InstallUninstall.Uninstall
                                                       })

    let componentSelector = fun (comp : Wix.Component) -> comp.Files |> Seq.exists(fun file -> file.Name.EndsWith(".exe"))

    let componentsWithOneServiceControl = Wix.attachServiceControlToComponents components componentSelector [serviceControl]
    let componentsWithOneServiceInstall = Wix.attachServiceInstallToComponents componentsWithOneServiceControl componentSelector [serviceInstall]

    let componentsRefs = Wix.getComponentRefs componentsWithOneServiceInstall

    let completeFeature = Wix.generateFeatureElement (fun f -> 
                                                        {f with 
                                                            Id = "DemoService"
                                                            Title = "Demo-Service"
                                                            Level = 1 
                                                            Description = "Installs the Demo-Service"
                                                            Components = componentsRefs
                                                            Display = Wix.Expand 
                                                            NestedFeatures = [] // no nested features yet
                                                        })

    // Generates a predefined WiX template with placeholders which will be replaced in "FillInWiXScript"
    Wix.generateWiXScript templateFileFullPath

    let WiXUIFeatureTree = Wix.generateUIRef (fun f ->
                                                {f with
                                                    Id = "WixUI_FeatureTree"
                                                })

    let MajorUpgrade = Wix.generateMajorUpgradeVersion(fun f ->
                                                         {f with 
                                                             Schedule = Wix.MajorUpgradeSchedule.AfterInstallExecute
                                                             DowngradeErrorMessage = "A later version is already installed, exiting."
                                                         })

    let WixVariables = [{Wix.Variable.Id="WixUILicenseRtf"; Wix.Variable.Overridable=Wix.No; Wix.Variable.Value="./../license.rtf"}]

    // wixPath "" means root folder
    // all scripts (*.wxs) in wixPath get populated with the given values
    // use different paths in case it needs different values
    Wix.fillInWiXTemplate workDir (fun f ->
                                     {f with
                                         // Guid which should be generated on every build
                                         ProductCode = System.Guid.NewGuid()
                                         ProductName = "Demo-Service"
                                         Description = "Setup for the Demo-Service."
                                         ProductLanguage = 1033
                                         ProductVersion = "19.0.0"
                                         ProductPublisher = "Fake/WiX Demo Company"
                                         // Set fixed upgrade guid, this should never change for this project!
                                         UpgradeGuid = (System.Guid WixProductUpgradeGuid)
                                         MajorUpgrade = [MajorUpgrade]
                                         UIRefs = [WiXUIFeatureTree]
                                         ProgramFilesFolder = Wix.ProgramFiles32
                                         // Directories = [virtualDbTemplateDir] // we don't need for this example
                                         Components = Seq.concat [componentsWithOneServiceInstall]
                                         BuildNumber = "Build number"
                                         Features = [completeFeature]
                                         WiXVariables = WixVariables
                                     })

    // run the WiX tools
    Wix.WiX (fun p -> {p with ToolDirectory = wixToolsPath}) 
        setupFileName
        templateFileFullPath
)

Target.create "All" ignore

"Nothing"
  ==> "Clean"
  ==> "Build-Service"
  ==> "BuildWiXSetup"
  ==> "All"

Target.runOrDefault "All"
