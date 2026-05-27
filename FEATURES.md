# Project Features and Change Log

This file tracks the features implemented and significant changes made to the project. It serves as a reference for future Gemini sessions and developers to understand the project's evolution.

## [2026-05-24] Initial Setup
- Created `FEATURES.md` to track project features and changes.
- Created `GEMINI.md` to mandate reading and updating `FEATURES.md` across all sessions.

## [2026-05-24] Excel Integration and Testing
- Added a 10x10 Excel file `TestTable.xlsx` to the project root.
- Migrated all unit tests to the `TestProjectEnbedded` sub-module.
- Updated `TestProjectEnbedded\UnitTest.cs` with the Excel reading test and existing range validation tests (NUnit).
- Cleaned up `EnbeddedRequirmentToTest.csproj` by removing temporary MSTest dependencies.

## [2026-05-24] Architectural Refactoring and Modularization
- Created a new sub-module `DocumentManager` to handle document reading and business logic.
- Moved `DocumentReader.cs`, `RangeValidator.cs`, and `TestData.cs` from the main project to `DocumentManager`.
- Consolidated `ValidateRange` logic from `Program.cs` into `RangeValidator.cs` to eliminate duplication.
- Updated `EnbeddedRequirmentToTest` and `TestProjectEnbedded` to reference the new `DocumentManager` module.
- Organized code with proper namespaces (`DocumentManager`).
- Moved `TestTable.xlsx` to the `TestProjectEnbedded` project to keep test assets with the tests.
- Updated `TestTable.xlsx` structure to 2 columns x 10 rows (Parameter Name and Range/Value).
- Column 2 now contains 4 comma-separated values (random mix of numbers 1-10 and words "Alpha", "Beta", "Gamma", "Delta").
- Translated all Hebrew comments, console messages, and garbled text to English across the entire project.
- Added a feature to generate `Requirements.docx` in the `TestProjectEnbedded` project.
- The Word document contains a table with an IF AND condition logic between Param1 and Param2 from the Excel file.

## [2026-05-25] Test Table Generation from Requirements
- Created a new sub-module `TestCreator` to handle test permutation generation.
- Implemented `ExcelTestTableCreator` to parse conditions from `Requirements.docx` (e.g., `If Param_1 > Param_2 then True else False`).
- The module extracts participating parameters from the condition and retrieves their possible values from `TestTable.xlsx`.
- Generates all permutations (Cartesian product) of parameter values and evaluates the condition for each.
- Integrated the new module into the main program and verified the output.

## [2026-05-25] Project Cleanup and Asset Reorganization
- Removed the `TestProjectEnbedded` sub-module as it was no longer needed.
- Created an `Assets` folder in the root directory to store essential test files (`Requirements.docx`, `TestTable.xlsx`).
- Updated all internal paths to point to the new `Assets` directory.
- Verified that both the CLI and Desktop application remain functional after the reorganization.

## [2026-05-25] Desktop Application Migration (WPF)
- Migrated the desktop application from Windows Forms to **Windows Presentation Foundation (WPF)** using C#.
- Created the `TestCreatorWpfApp` project.
- **UI Design**:
  - Leveraged XAML for a modern and flexible layout.
  - Used a `Grid` with `DockPanel` and `UniformGrid` for responsive positioning.
  - Maintained TextBoxes for path selection and added "Browse" buttons for each field.
- **Features**:
  - Integrated `OpenFileDialog` and `SaveFileDialog` for file selection.
  - Retained the **15-row preview feature** using a `ListBox` with monospace font (`Consolas`) and dynamic padding for alignment.
  - Connected the execution logic to the `TestCreator` and `DocumentManager` modules.
- Set the WPF application as the default startup project in the solution.

## [2026-05-25] UDP Echo Verification and Color-Coded UI
- Enhanced the `ConectionManager` module with UDP echo capabilities.
- Added `UdpEchoServer` class for local packet echoing.
- Updated `UdpSocketManager` to support asynchronous sending and receiving with timeouts.
- **WPF Application Integration**:
  - The "Execute" button now triggers a real-time UDP verification process.
  - For each generated test permutation row, the application sends the data to the local UDP server.
  - **Visual Feedback**: The ListBox preview now uses color-coding:
    - **Green**: Successfully sent and received an identical echo response.
    - **Red**: Failed transmission, timeout, or corrupted echo.
  - Implemented a custom `PreviewRow` data model to support dynamic color binding in the XAML UI.

## [2026-05-25] External UDP Terminals and Lifecycle Management
- Created a new console sub-module `UdpTerminalTool` to act as standalone UDP nodes.
- **Terminal Integration**:
  - The WPF application now automatically launches two external terminal windows at startup:
    - **UDP Echo Server**: Runs the echo logic in a visible console.
    - **UDP Monitor Client**: A secondary console window for monitoring.
  - **Lifecycle Management**: The application tracks these process IDs and ensures both terminals are killed automatically when the main WPF window is closed.
- Updated `MainWindow.xaml.cs` to handle external process orchestration and clean shutdown.

## [2026-05-25] Enhanced Generation Options and Results Chart
- Renamed "Execute" button to **"Create & Execute"**.
- Added a new **"Create test only"** button for generating files without network verification.
- **Visual Results Summary**:
  - Added a **dynamic bar chart** above the action buttons.
  - The chart visually represents the ratio of **Passed** (Green) vs **Failed** (Red) UDP echo tests.
  - Includes text labels showing the exact count of successes and failures.
  - The chart updates in real-time as each row is verified.
- Added a **"Test Echo"** button for manual connection verification.

## [2026-05-25] Modern UI Overhaul and Path Persistence
- **High-End UI Redesign**:
  - Transformed the application into a modern dashboard inspired by DevExpress/Fluent design.
  - Added a dark **Sidebar Navigation** for branding and quick access.
  - Organised content into **Shadowed Cards** for a professional enterprise feel.
  - Implemented a cohesive **Purple and Emerald Green** color palette.
- **Persistent Settings**:
  - Integrated `System.Text.Json` to save and load user-selected file paths.
  - The application now **remembers the last used paths** for Requirements, Parameters, and Output files across sessions.
  - Settings are automatically updated upon successful file browsing or test execution.
- Optimized window layout for better readability on various screen sizes.

## [2026-05-25] Advanced Logic Parser for Complex Conditions
- Upgraded the `TestCreator` module with a robust **LogicEvaluator** engine.
- **Support for Complex Logic**: The parser now handles nested conditions including:
  - **Boolean Operators**: `AND`, `OR`, `XOR`, `NOT`.
  - **Parentheses**: Supports complex grouping (e.g., `(Param_1 > 5 AND Param_2 == "Beta") OR Param_3 != 10`).
  - **Full Comparison Set**: Supports `==`, `!=`, `>`, `<`, `>=`, `<=`.
- **Intelligent Substitution**: Automatically replaces parameter tokens with their actual values from the Excel source, handling both numeric and string comparisons seamlessly.
- **Error Handling**: Gracefully handles parsing errors and logs feedback to the UI/Terminal.

## [2026-05-25] Dual Protocol Support (TCP/UDP) and Enhanced History/Settings
- **TCP Networking Integration**:
  - Expanded `ConnectionManager` with `TcpEchoServer` and `TcpSocketManager`.
  - Added support for reliable TCP communication alongside UDP.
- **Protocol Selection**:
  - Added a **Checkbox** to the main dashboard allowing users to toggle between TCP and UDP for test execution and echo testing.
- **Enhanced Settings**:
  - Updated the Settings view to allow independent configuration of **TCP and UDP IP/Port** parameters.
  - Added a dedicated **Database Configuration** section to persist a DB connection string.
- **Advanced Action History**:
  - Implemented a **History View** tracking the last 5 operations.
  - Operations include details on which protocol (TCP/UDP) was used.
  - Added a **"Clear History"** functionality.
- **Terminal Orchestration**: Updated `UdpTerminalTool` to support multiple modes (`server`, `client`, `tcpserver`, `tcpclient`), enabling up to 4 concurrent monitoring windows managed by the WPF app.

## [2026-05-25] Standardized Timestamps and Real-time Logging Fixes
- Added **Date and Time stamps** to all system outputs for better traceability.
- **Real-time Console Output**: Fixed a buffering issue by forcing `AutoFlush` and manual flushes in all server and terminal components.
- **Client-Side Monitor**: Updated external monitors to display "Sending" events in real-time, providing full end-to-end visibility.

## [2026-05-26] SQL Database Parameter Loading and Dynamic UI
- **SQL Integration**: Added the ability to load test parameters directly from a SQL Database.
- **ConnectionManager Enhancement**: Updated `DatabaseManager` with `LoadParametersAsync` to fetch data from a `TestParameters` table.
- **TestCreator Overload**: Added support in `ExcelTestTableCreator` to accept parameter data as a dictionary, enabling sources other than Excel.
- **WPF UI Update**:
  - Added a "Load Parameters from SQL" checkbox to the main dashboard.
  - **Dynamic State Management**: Checking the SQL toggle now automatically disables (greys out) the Excel parameter input section.
  - **Context-Aware Labels**: The "Test Parameters" label now dynamically changes its suffix from "(.xlsx)" to "(DB)" when SQL loading is active, providing clear visual feedback.
  - Updated validation logic to ensure required inputs (paths or connection string) are present before execution.
  - Persistent settings now include the state of the SQL toggle.

## [2026-05-26] JSON Parameter Loading Support
- **DocumentManager Enhancement**: Created `JsonParameterReader` class to parse JSON files into parameter dictionaries.
- **Format Support**: Supports JSON objects where keys are parameter names and values are lists of string values.
- **WPF Application Integration**:
  - Updated "Test Parameters" browse button to support `.json` file selection.
  - Integrated `JsonParameterReader` into the execution workflow to handle JSON input.
  - The application now supports three parameter sources: Excel (.xlsx), JSON (.json), and SQL Database.

## [2026-05-26] Requirement Preview Feature
- **WPF UI Update**: Added a new "Loaded Requirements" card to the dashboard.
- **Real-time Extraction**: Implemented logic to automatically extract and display the condition text from the selected `.docx` file.
- **Interactive Feedback**: The preview updates instantly when a new requirements file is selected or when the application starts with a saved path.

## [2026-05-26] Real-time Progress Tracking
- **Execution Monitoring**: Added progress bars to each row in the real-time monitor to indicate individual test status.
- **Total Progress**: Added a global progress bar at the top of the monitor to track the overall completion percentage of the test suite.
- **Dynamic Updates**: Integrated `INotifyPropertyChanged` in the `PreviewRow` model to ensure the UI updates instantly as tests proceed from "Started" to "Finished".

## [2026-05-27] "Record Testing" Tab Implementation
- **New Navigation**: Added a "Record Testing" button to the sidebar navigation in the WPF application.
- **Placeholder View**: Created a new content view for the "Record Testing" tab with a modern dashboard look.
- **Dynamic Navigation**: Updated the main window navigation logic to support toggling the visibility of the new tab.
- **Future-Ready**: The tab is structured to support future real-time testing session recording and monitoring.

## [2026-05-28] Post-Git Upload Fixes
- **Restored Solution Integrity**: Fixed 270+ build errors in Visual Studio following the project's upload to Git.
- **Path Correction**: Updated `EnbeddedRequirmentToTest.sln` and `EnbeddedRequirmentToTest.csproj` to reflect the new directory structure (moving sibling projects into subdirectories).
- **Source Conflict Resolution**: Configured the root project (`EnbeddedRequirmentToTest.csproj`) to exclude sub-project directories (`DocumentManager`, `TestCreator`, etc.) from its recursive compilation to prevent duplicate definition errors.
- **Verified Build**: Confirmed successful solution-wide build using the CLI and addressed broken project references.

- **Sub-module Creation**: Developed the `SchemaDiscovery` module for advanced SQL schema exploration.
- **Database Analysis**: The "Query" button now retrieves selected table structures and fetches real-time Min/Max statistics for columns.
- **Telemetry Integration**: Implemented logic to load parameter data from `telemetria.xlsx` and cross-reference it with database metadata.
- **Interactive Visualization**: Replaced static `Polyline` with `LiveCharts.Wpf` for professional and high-performance data rendering.
- **Sparkline Graphs**: Each parameter now features a dynamic LiveCharts `CartesianChart` representing data trends, with status indicators (Normal/Alert) based on DB boundaries.
- **Multi-Source Logic**: Synchronized query logic with the test creation engine to allow requirement-aware data analysis.

## [2026-05-26] Targeted Test Execution and Multi-Selection
- **Run Selected Test**: Added a **"Run Selected"** button to the Execution Monitor.
- **Batch Re-testing**: Enabled **multi-selection** in the monitor list (using Ctrl/Shift or click-drag).
- **Sequential Execution**: When multiple rows are selected, the "Run Selected" button executes all chosen test cases sequentially.
- **Real-time Row Feedback**: Each selected row updates its progress and status independently during the batch run.

## [2026-05-26] 3D Infographic Chart with 4 Indicators
- **Advanced Visualization**: Upgraded the 3D chart to include 4 distinct pillars: **Pass, Fail, Not Run, and Warning**.
- **State Definition**:
  - **Pass (Green)**: Successful network echo.
  - **Warning (Amber)**: Network echo received but data did not match exactly (corrupted).
  - **Fail (Red)**: Network timeout or no response.
  - **Not Run (Slate Gray)**: Permutations currently pending execution.
- **Pillar & Stage Layout**: Optimized the 3D stage and camera to fit 4 indicators side-by-side with professional lighting and materials.
- **Side-by-Side Monitoring**: Reorganized the dashboard layout to place the 3D chart to the right of the execution log for better spatial efficiency.
