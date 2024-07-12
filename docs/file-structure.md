# File Structure

This file contains the file structure of the C# project file.

# Table of Contents

- [client Folder](file-structure.md#client-folder)
    - [data Folder](file-structure.md#data-folder)
        - [Methods Folder](file-structure.md#methods-folder)
            - [BasicInfo.cs](file-structure.md#basicinfocs)
            - [Hardware.cs](file-structure.md#hardwarecs)
            - [Network.cs](file-structure.md#networkcs)
            - [Security.cs](file-structure.md#securitycs)
            - [System.cs](file-structure.md#systemcs)
        - [Cache.cs](file-structure.md#cachecs)
        - [Structs.cs](file-structure.md#structscs)
        - [Utils.cs](file-structure.md#utilscs)
    - [Frontend Folder](file-structure.md#frontend-folder)
        - [Fonts Folder](file-structure.md#fonts-folder)
        - [Images Folder](file-structure.md#images-folder)
        - [EndScreen Folder](file-structure.md#endscreen-folder)
        - [Landing.xaml](file-structure.md#landingxaml)
        - [StartButtons.xaml](file-structure.md#startbuttonsxaml)
        - [Run.xaml](file-structure.md#runxaml)
    - [Program.cs](file-structure.md#programcs)
    - [Settings.cs](file-structure.md#settingscs)
    - [Progress.cs](file-structure.md#progresscs)
    - [Monolith.cs](file-structure.md#monolithcs)
    - [DebugLog.cs](file-structure.md#debuglogcs)
    - [Interop.cs](file-structure.md#interopcs)

## [Client Folder](/client/)

The actual folder containing the project file (client.sln / specify_client.csproj).

### [data Folder](/client/data/)

Folder that contains the methods and classes for the output.

#### [Methods Folder](/client/data/Methods/)

Contains the code for collecting specs.

##### [BasicInfo.cs](/client/data/Methods/BasicInfo.cs)

Gets the most basic information about the computer. Responsible for the output of `Edition`, `Version`, `Uptime`, `InstallDate`, etc.

##### [Hardware.cs](/client/data/Methods/Hardware.cs)

Gets information about the hardware of the computer. Responsible for the output of `Ram`, `Cpu`, `Gpu`, `Motherboard`, etc.

##### [Network.cs](/client/data/Methods/Network.cs)

Gets information about the current network configuration of the computer. Responsible for the output of `Adapters`, `Routes`, `HostsFile`, `NetworkConnections`, etc.

##### [Security.cs](/client/data/Methods/Security.cs)

Gets information about the current security configuration of the computer. Responsible for the output of `AvList`, `UacEnabled`, `SecureBootEnabled`, `Tpm`, etc.

##### [System.cs](/client/data/Methods/System.cs)

Gets information about the current operating system install of the computer. Responsible for the output of `UserVariables`, `SystemVariables`, `RunningProcesses`, `InstalledApps`, etc.


#### [Cache.cs](/client/data/Cache.cs)

Contains the class in which most of the collected info from Methods are stored. Cache.cs only receives and contains info that results in strings, booleans, and numbers for organization.

#### [Structs.cs](/client/data/Structs.cs)

Contains the class in which the collected info that are lists are stored. Separated from Cache.cs for (again) organization.

#### [Utils.cs](/client/data/Utils.cs)

Contains methods that are frequently used in the Methods folder. For example, the code for `GetWMI()` is stored in here.

### [Frontend Folder](/client/Frontend/)

Contains all the GUI stuff.

#### [Fonts Folder](/client/Frontend/Fonts/)

Contains [AldoTheApache.ttf](/client/Frontend/Fonts/AldotheApache.ttf), which is used in the end screens.

#### [Images Folder](/client/Frontend/Images/)

Contains the magnifying glass animations (loop(number).gif), and other images, saved as SVGs (Images.xaml).

#### [EndScreen Folder](/client/Frontend/EndScreen/)

Contains the frames when Specify finishes.

#### [Landing.xaml](/client/Frontend/Landing.xaml)

Is the Main Window of the GUI. Contains the logo and the corner hyperlink.

#### [StartButtons.xaml](/client/Frontend/StartButtons.xaml)

Contains the toggle buttons that controls the settings.

#### [Run.xaml](/client/Frontend/Run.xaml)

Contains the loading screen. It's codebehind calls Program.cs to run the program.

### [Program.cs](/client/Program.cs)

Program.cs contains the method that triggers Progress.cs to do the info collection.

### [Settings.cs](/client/Settings.cs)

This has the class that stores the settings set by the user. It modifies Monolith.cs's behavior depending on the set variables.

### [Progress.cs](/client/Progress.cs)

Progress.cs contains the code that triggers the files in the Methods Folder (collection), and Monolith.cs (Upload / Export).

### [Monolith.cs](/client/Monolith.cs)

Monolith.cs contains the code that exports it to a JSON (`Serialize` and `Specificialize`) and uploads it to spec-ify.com (`DoRequest`).

### [DebugLog.cs](/client/DebugLog.cs)

DebugLog.cs contains the code for the debug log's functions. When the proper setting is enabled, each line in the process, step by step, is recorded.

### [Interop.cs](/client/Interop.cs)

Interop.cs contains methods that query specific Windows dlls for info. Probably shouldn't touch this if you don't know what you're doing.
