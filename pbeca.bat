Rem Builds the ECA (Elementary Cellular Automaton) DLL and copies it to the main program folder
dotnet build SimLabECA\SimLabECA.csproj
copy SimLabECA\bin\Debug\net9.0\SimLabECA.dll SimLab\
copy SimLabECA\bin\Debug\net9.0\SimLabECA.dll SimLab\bin\Debug\net9.0\