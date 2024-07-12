# Creating a new Specify release

This is the process of how we do our releases.

Ensure your local repository is up-to-date, then update the version number to the version desired:
- In `./specify/Program.cs`, update `SpecifyVersion`
- In `./specify/Properties/AssemblyInfo.cs`, update `AssemblyVersion` and `AssemblyFileVersion`

Create a new commit named nothing but the version number:

```
git commit -am "v9.9.9"
```

Push these changes to GitHub.

## Building 

In Visual Studio, select `Debug`, and then switch it to `Release`, then build. This will compile the program to a single .exe file.

## Signing

Contact the person managing signing, currently Drei, send them the file `./specify/client/bin/Release/specify_client.exe`, and wait for them to sign it.

## Uploading

Go to the GitHub repository, select `Releases`, and click `New Draft Release` to create a Release Draft.

- Set the release title for your draft to the version (`v1.1.2`).

- Select `Attach binaries by dropping them here or selecting them.` then upload the .exe file the person currently managing signing sent back. Make sure to name it `Specify.exe` so that old links that link to the latest release will still work.

- Add the commit changelog by adding `Full Changelog: v1.1.1...v9.9.9`, replacing the former with the last version, and the latter with the current version 

- Add a simplification of that changelog like:
```
- GUI redesign
- Better AV filter
- Get SMART data for NVMe drives 
```

- Toggle the option `Set as the latest release` to do what it says. This will make the links stated previously to link to the binary you uploaded here.