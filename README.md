# SimLab: Platform for creating and testing of cellular automata

SimLab (Simulation Laboratory) is a general-purpose platform for creating and testing cellular automata-based systems, with support for 1D, 2D, and 3D worlds, plug-in based simulation logic, visualization, and database-backed state persistence.

Full source code: https://github.com/tijana-stankovic/SimLab

## Features

- Plug-in architecture (`SimLabApi`) for custom simulation logic
- Multiple simulation phases (`Initialization`, `PreCycle`, `ProcessWorld`, `Update`, `Evaluation`, `Reproduction`, `Selection`, `PostCycle`)
- Synchronous and asynchronous simulation modes
- Visualization with frame history navigation
- PostgreSQL persistence for worlds and simulation history
- Example plug-ins:
    - Game of Life (`SimLabGOL`)
    - ECA (`SimLabECA`)
    - ECA 2D visualization (`SimLabECA_2D`)
    - ECA + GA demo (`SimLabGA`)

## Requirements

- Windows (current primary development environment)
- .NET SDK 9.0
- PostgreSQL

## Build

From the root SimLab folder:

```bash
dotnet build SimLabApi
dotnet build SimLabGOL
dotnet build SimLabECA
dotnet build SimLabECA_2D
dotnet build SimLabGA
dotnet build SimLab
```

## Run

Note: Configure database connection in **SimLab/DatabaseConfig.json**  

Navigate to SimLab/SimLab subfolder and execute command:

```bash
dotnet run --project SimLab.csproj
```

## Quick Start

1. Configure database connection in **SimLab/DatabaseConfig.json**.
2. Start SimLab.
3. Add a world from JSON config:
    - `WORLD ADD GOL.json`
4. Initialize / run simulation:
    - `SIMULATION NEXT`  
    or
    - `SIMULATION NEXT <n>`
5. Open visualization:
    - `SHOW`

## Example Configs

- SimLab/GOL.json
- SimLab/ECA.json
- SimLab/ECA_2D.json
- SimLab/GA.json

## License

MIT License. See LICENSE.
