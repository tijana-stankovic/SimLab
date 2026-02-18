Rem Builds the plugin2 DLL and copies it to the main program folder
dotnet build SimLabPlugIn2\SimLabPlugIn2.csproj
copy SimLabPlugIn2\bin\Debug\net9.0\SimLabPlugIn2.dll SimLab\
copy SimLabPlugIn2\bin\Debug\net9.0\SimLabPlugIn2.dll SimLab\bin\Debug\net9.0\
