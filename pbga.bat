Rem Builds the ECA with GA (Evolving Cellular Automata with Genetic Algorithms) DLL and copies it to the main program folder
dotnet build SimLabGA\SimLabGA.csproj
copy SimLabGA\bin\Debug\net9.0\SimLabGA.dll SimLab\
copy SimLabGA\bin\Debug\net9.0\SimLabGA.dll SimLab\bin\Debug\net9.0\