# S7 Simulator (WPF)

[中文文档](README_CN.md)

A powerful Siemens S7-1200/1500 PLC simulator built with WPF and Sharp7. This tool allows developers and engineers to simulate a PLC server, import DB structures from Excel, and interact with memory areas (DB, M, I, Q) via the standard S7 protocol.

## Features

- **S7 Server Simulation**: Acts as a fully functional Snap7 server listening on port 102.
- **Memory Areas**: Supports Read/Write operations on:
  - **DB**: Data Blocks (Dynamic registration based on import)
  - **MB**: Merkers / Flags
  - **IB**: Inputs
  - **QB**: Outputs
- **Excel Import**: Easily import DB definitions and variable tags from Excel files.
- **Persistence**: Automatically saves DB structures and variable values to a local SQLite database, ensuring data persists across restarts.
- **Real-time Monitoring**: View and edit variable values in real-time through a modern WPF interface.
- **Data Type Support**:
  - **BOOL**: Bit access
  - **BYTE, INT, DINT**: Integer types
  - **REAL**: Floating point
  - **STRING**: S7 Strings (UTF-8 support for Chinese characters)

## Tech Stack

- **Framework**: .NET 8 (WPF)
- **Architecture**: MVVM (Model-View-ViewModel) using `CommunityToolkit.Mvvm`
- **PLC Communication**: `Sharp7` (C# port of Snap7)
- **Database**: `SQLite` (`Microsoft.Data.Sqlite`)
- **Excel Processing**: `EPPlus`

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or a compatible IDE

### Installation

1. Clone the repository.
2. Open `S7Simulator.Wpf.sln`.
3. Restore NuGet packages and build the solution.

### Usage

1. **Start the Simulator**: Run the application. The status bar will indicate "S7 Server Started".
2. **Import DBs**:
   - Click **"导入 Excel (DB/Tags)"**.
   - Select an Excel file containing your DB definitions.
   - **Excel Format**:
     - Column 1: DB Number
     - Column 2: Variable Name
     - Column 3: Data Type (BOOL, INT, REAL, STRING, etc.)
     - Column 4: Byte Offset
     - Column 5: Bit Offset (for BOOL, else empty/-1)
     - Column 6: Initial Value
     - Column 7: Comment
3. **Connect Client**:
   - Use any S7 Client (e.g., HMI, SCADA, another PLC, or Snap7 Client).
   - Connect to the machine's IP address (or `127.0.0.1` for local testing).
   - Rack: 0, Slot: 1 (Standard S7-1200/1500 addressing).
4. **Monitor & Edit**:
   - Double-click a DB in the list to open the **Detail View**.
   - View real-time values updating every 100ms.
   - Edit values directly in the grid to simulate field changes.

## Project Structure

- **`S7Simulator.Wpf`**: Main application project.
  - **`Models`**: Data entities (`DbInfo`, `VariableInfo`).
  - **`ViewModels`**: Application logic (`MainViewModel`).
  - **`Views`**: UI components (`MainWindow`, `DBDetailWindow`).
  - **`Services`**:
    - `DatabaseService`: Async SQLite operations for persistence.
    - `ExcelImporter`: Async Excel parsing logic.
  - **`Plc`**:
    - `PlcServer`: Manages the Snap7 server lifecycle and area registration.
    - `PlcMemory`: Thread-safe memory buffer management and type conversion.

## Contributing

Contributions are welcome! Please submit a Pull Request or open an Issue for any bugs or feature requests.

## License

This project is licensed under the MIT License.
