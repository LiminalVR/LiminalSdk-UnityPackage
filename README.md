# Liminal-SDK-Unity-Package

## About

This is a repository containing the Liminal SDK as a Unity Package. This package is used to bundle `scripts`, a `Unity Scene` and `Assets` into a `.limapp` file. The `.limapp` file is required for the Liminal Platform to run as one of our `Experiences`. 

### Unity Version
Unity **6000.7.0b2 (Unity 6.7 beta)**. The primary Android target is **Meta Quest 3**.

Use Android Build Support with the editor's SDK, NDK and OpenJDK. XR runs through
Unity OpenXR, XR Interaction Toolkit and XR Hands; their versions are declared in
`package.json`. The old Oculus XR provider is no longer used. The bundled legacy
OVR/SteamVR types remain for existing SDK references; legacy SteamVR device-name
startup is not supported in Unity 6.7. Use the UnityXR avatar and OpenXR for Quest
and desktop Link.

In **Liminal > Build Window > Setup**, configure XR and Android. Quest builds use
IL2CPP, ARM64, Game Activity, Linear color, Android API 30 or newer, and OpenGL ES 3.
GLES3 is retained to match the Liminal platform and its limapp shaders. Quest uses
Single Pass Instanced; desktop Link retains the existing Multi Pass setting.
Meta Quest Support, Touch/Touch Plus controller profiles and hand tracking must
be enabled. Build the platform player and its limapps with matching Unity and XR
versions; rebuild existing bundles for the new editor.

## Setting Up

The _Liminal SDK_ provides its own copies of the Oculus VR & Gear VR SDKs. Before importing the _Liminal SDK_, check that these SDKs are not already included in your project:

> - OVR (Oculus)
> - GVR (Google VR)
> - Liminal

If you have any of the SDKs above: 

> 1. Close Unity
> 2. Delete the SDK files from your project via your file browser. 

**Important:** You must close Unity, otherwise the SDKs will auto-generate again.

### Setup Git
> - If you do not have Git installed, Download Git https://git-scm.com/downloads
> - Once git has been installed, open Comand Prompt and type git, it should recognize git. 
> - Confirm it by going opening command prompt (windows key > cmd ) then type git. It should give you a start with `usage: git`

#### In the case that it does not work, you will have to do the following.
> - Otherwise, locate yourpath/cmd/git.exe, copy the path to just yurpath/cmd/
> - Add the path to cmd to your Environment Variables. (Press Window, Search Environment Variables)

*Important* If this is your first time adding a git path to your environment variable, you must ensure all Unity instances are closed after setting up git. Alternatively, you can just restart your machine.

### Importing the _Liminal SDK_

The Liminal SDK uses Unity Package Manager.

> 1. Locate YourProjectName/Packages/manifest.json
> 2. Include Liminal SDK by adding the package name and git path. At the top, add `"com.liminal.sdk": "https://github.com/LiminalVR/Liminal-SDK-Unity-Package.git",`
> 3. Open the Unity Project and the Liminal SDK will automatically be imported.
> 4. Open **Liminal > Build Window > Setup** and configure XR and Android.
> 5. Review **Project Settings > XR Plug-in Management > Project Validation** for Android.

For automation, run Unity with `-batchmode -nographics -quit -buildTarget Android`
and `-executeMethod Liminal.SDK.Build.QuestBuildValidation.Configure`. Run a second
invocation with `Liminal.SDK.Build.QuestBuildValidation.Validate` after the define
changes have compiled. `Liminal.SDK.Build.QuestBuildValidation.BuildSmokePlayer`
also builds a development APK; supply `-liminalSmokeScene Assets/path/to/scene.unity`
and `-liminalSmokeOutput /path/to/smoke.apk`. The smoke APK uses
`com.liminalvr.sdk.smoketest` and does not install or launch automatically.
In the SDK development repository, `-liminalExcludeLegacyFixtures` excludes
`Assets/TestApp/DLLs` from that build only. Use it for SDK smoke testing when the
legacy experience binaries have not yet been rebuilt; it does not certify those
experiences or change their importer settings.

### Updating the SDK

**Resolving**: This process may take a bit of time as it is downloading the package from git.

From Unity Editor toolbars:

> `Liminal > Update Package`

## Troubleshooting

- All files must have .meta
