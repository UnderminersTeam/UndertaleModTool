This folder contains data for specific games, to improve the modding experience when using certain features.

Games are defined in the "Definitions" sub-folder, where they are given conditions to match the game(s) they target. Any JSON files from this folder are loaded automatically, if available.

GML decompiler configs are contained within the "Underanalyzer" sub-folder, referenced from the original definition files. (These are loaded on-demand.)

If you want to load this data from other path (or you just don't need to work with GML code), then you can prevent automatic folder copying.
For that, you can do one of the following:
1) Edit the `CopyGameSpecificDataToOutDir` project property (change to "false").
2) Override its value through "Directory.Build.props" file outside of the project (e.g. when you use "UndertaleModLib" as a git submodule).
Create the file (if doesn't exists) in the solution root folder, and write the following:
<Project>
  <PropertyGroup>
    <CopyGameSpecificDataToOutDir>false</CopyGameSpecificDataToOutDir>
  </PropertyGroup>
</Project>

In order to change the "GameSpecificData" folder location, you should change the `GameSpecificResolver.BaseDirectory` property.