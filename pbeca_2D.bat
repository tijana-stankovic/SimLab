Rem Builds the ECA 2D (Elementary Cellular Automaton in 2D) DLL and copies it to the main program folder
dotnet build SimLabECA_2D\SimLabECA_2D.csproj
copy SimLabECA_2D\bin\Debug\net9.0\SimLabECA_2D.dll SimLab\
copy SimLabECA_2D\bin\Debug\net9.0\SimLabECA_2D.dll SimLab\bin\Debug\net9.0\