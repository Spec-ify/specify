# Overview the Specify client

## Summary
The Specify client is written in C#, and targets .NET Framework version 4.7.2. It collects system info and automatically uploads a JSON file of the results to the [Specify Viewer](https://spec-ify.com), referred internally as [Specified](https://github.com/Spec-ify/specified). 

If it fails or is manually set to do otherwise, the program will instead generate `specify_specs.json` in the same directory as the executable. This can be then uploaded to a JSON viewer like [spec-ify.com](https://spec-ify.com/) or browsed manually. 
