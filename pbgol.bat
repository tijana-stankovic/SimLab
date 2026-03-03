Rem Builds the Game of Life plugin DLL and copies it to the main program folder
dotnet build SimLabGOL\SimLabGOL.csproj
copy SimLabGOL\bin\Debug\net9.0\SimLabGOL.dll SimLab\
copy SimLabGOL\bin\Debug\net9.0\SimLabGOL.dll SimLab\bin\Debug\net9.0\