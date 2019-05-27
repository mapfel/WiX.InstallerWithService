# Description of the Fake/WiX Installer Demo

## Introduction

This is an example how to create an installer for a **Windows** service with **Fake** and allied tools.

## Overview

The demo is based on:

- [Fake](https://fake.build/) for build scripting
- [Fake WiX Module](https://fake.build/apidocs/v5/fake-installer-wix.html) to provide an easy way to create a [WiX](https://wixtoolset.org/) based Windows installer
- [Paket] for dependency management
- **Visual Studio** project template for _Windows Service (.NET Framework)

The overall workflow to build the `Setup.msi` uses 3 steps (targets) in **Fake**

1. `Clean` - deletes old artifacts
1. `Build-Service` - compiles the .NET Demo Service
1. `BuildWiXSetup` - creates the installer based on **WiX**

This demo focusses the **WiX** related functionality, so only that part is described in more detail here.

## Quickstart

1. open a developer console
1. navigate to the project root folder
1. run `./build/build.cmd`
1. the installer is inside `./deploy/` folder 

## Details

The **WiX** related script parts came originally from [Create WiX Setup](https://fake.build/todo-wix.html#Create-WiX-Setup).

So the example also uses the approach to create a template with placeholders (`Wix.generateWiXScript`) which gets later populated with real values (`Wix.fillInWiXTemplate`). This transforms the template to a ready to use `.wxs` (WiX Source) file, which is the input for `Wix.WiX` to create the setup.

A service element needs to get enriched with these elements:

- [ServiceControl](https://wixtoolset.org/documentation/manual/v3/xsd/wix/servicecontrol.html)
- [ServiceInstall](https://wixtoolset.org/documentation/manual/v3/xsd/wix/serviceinstall.html)

Two functions are responsible to create these structures:

- [Wix.generateServiceControl](https://fake.build/apidocs/v5/fake-installer-wix.html#Functions%20and%20values)
- [Wix.generateServiceInstall](https://fake.build/apidocs/v5/fake-installer-wix.html#Functions%20and%20values)

In the next step these structures get attached to the service element via

- [attachServiceControlToComponents](https://fake.build/apidocs/v5/fake-installer-wix.html#Functions%20and%20values)
- [attachServiceInstallToComponents](https://fake.build/apidocs/v5/fake-installer-wix.html#Functions%20and%20values)

The `componentSelector` finds the element where the attaching has to be done. It filters for the `DemoService.exe` file element.

## Service Management

### Install the Service

1. create an account for service (or use an existing account)
    - use `lusrmgr.msc`
1. ensure necessary permissions to run as a service
    - run `secpol.msc`
    - navigate to _Security Settings > Local Policies > User Right Assignments_
    - open policy _Log on as a service_
    - add account to list of users
1. install the service
    - open command line (preferably with administrative permissions)
    - run installer with parameters
      ```batch
      msiexec /i Setup.msi SVC_USER_ACCOUNT="<account-name>" SVC_USER_PASSWORD="<account-password>"
      ```
    - see help of `msiexec` for further options and parameters (e.g. `/quiet`)

If you don't specify an account name (e.g. by running the installer in UI mode) the service gets created under _Local System_.

### Uninstall the Service

- open command line (preferably with administrative permissions)
- run installer with parameters
  ```batch
  msiexec /x Setup.msi
  ```

Alternatively you can run the installer a second time. This opens a new dialog which offers a remove option.

## Further Prospects

Currently the dynamically created template (`Wix.generateWiXScript`) doesn't support the definition of application icons which are shown is the _Programs and Features_ rubric of the _Control Panel_.

An easy workaround could be

1. saving of the template before it gets populated with values
1. adding the necessary specification to include the icon (`*.ico` file) (e.g. before the `Media` element)

    ```xml
    <Icon Id="icon.ico" SourceFile="Icon.ico"/>
    <Property Id="ARPPRODUCTICON" Value="icon.ico" />
    ```

## Troubleshooting

### Workspace cleanup

- ensure, that you don't have uncommitted changes you like to keep
- run `git clean -dxf` to cleanup your workspace

### Service doesn't get uninstalled

- ensure your run the uninstall command from a console under administrative permissions
