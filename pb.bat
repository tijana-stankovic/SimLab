Rem Builds the plugin DLL and copies it to the main program folder
dotnet build SimLabPlugIn\SimLabPlugIn.csproj
copy SimLabPlugIn\bin\Debug\net9.0\SimLabPlugIn.dll SimLab\
copy SimLabPlugIn\bin\Debug\net9.0\SimLabPlugIn.dll SimLab\bin\Debug\net9.0\