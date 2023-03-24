# Creating a new Specify release
Ensure your local repository is up to date, then update the version number to the version desired:
- In `./specify/Program.cs`, update `SpecifyVersion`
- In `./specify/Properties/AssemblyInfo.cs`, update `AssemblyVersion` and `AssemblyFileVersion`

Create a new commit named nothing but the version number:
```
git commit -am "v9.9.9"
```

Push those changes to GitHub, then go select `New Draft Release` under `Releases`

## Building
In Visual Studio, select `Debug`, and then toggle to `Release`, then build. 

## Signing
Contact the person managing signing, currently Drei and wait for them to sign it

## Uploading
- Select the edit icon for the release if needed (small pencil icon on top right)
- Select `Attach binaries by dropping them here or selecting them.` then upload `./specify/client/bin/Release/specify_client.exe`.
- Set as Latest Release if necessary.
