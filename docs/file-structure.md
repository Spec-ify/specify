# File Structure

This file contains the file structure of the C# project file.

## [Client Folder](/client/)

The actual folder of the project file. Self-explanatory.

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

Contains the class in which most of the collected info from Methods are stored. Cache.cs only receives and contains info that results in strings, bools, and numbers for organization.

#### [Structs.cs](/client/data/Structs.cs)

Contains the class in which the collected info that are lists are stored. Separated from Cache.cs for (again) organization.

#### [Utils.cs](/client/data/Utils.cs)

Contains methods that are frequently used in the Methods folder. For example, the code for `GetWMI()` is stored in here.

### [Program.cs](/client/Program.cs)

Program.cs contains the method that triggers Progress.cs to do the info collection, and (in the text-based UI) contains the input and output.

### [Settings.cs](/client/Settings.cs)

This has the class that contains.. well.. the settings. It modifies Monolith.cs's behavior depending on the set variables.

### [Progress.cs](/client/Progress.cs)

Progress.cs contains the code that triggers the files in the Methods Folder (collection), and Monolith.cs (Upload / Export). Also contains (in the text-based UI) the colors for the status texts.

### [Monolith.cs](/client/Monolith.cs)

Monolith.cs contains the code that exports it to a JSON (`Serialize` and `Specificialize`) and uploads it to spec-ify.com (`DoRequest`).

### [DebugLog.cs](/client/DebugLog.cs)

DebugLog.cs contains the code for... well.. the debug log. When the proper setting is enabled, each line in the process, step by step, is recorded.

### [Interop.cs](/client/Interop.cs)

Interop.cs contains methods that query specific Window dlls for info. Probably shouldn't touch this if you're inexperienced.
