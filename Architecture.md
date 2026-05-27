# Application Architecture & Dependencies

## 1. Modular Structure
The application follows a decoupled, project-based architecture to ensure maintainability and testability.

### [TestCreatorWpfApp] - Presentation Layer
- Central Dashboard (WPF)
- UI/UX Management (XAML)
- External Process Orchestration (Terminal Lifecycles)
- Configuration Persistence (settings.json)

### [TestCreator] - Core Logic Engine
- **LogicEvaluator**: Recursive boolean expression parser (AND, OR, XOR, NOT, Grouping).
- **ExcelTestTableCreator**: Generates test permutations using Cartesian product of parameter sets.

### [DocumentManager] - Data Access Layer
- Handles parsing of .docx (Requirements) and .xlsx (Parameter Tables).
- Provides data validation and transformation utilities.

### [ConnectionManager] - Communication Layer
- Asynchronous TCP/UDP Socket Managers.
- TCP/UDP Echo Server implementations for local loopback verification.
- **DatabaseManager**: SQL Server integration via Microsoft.Data.SqlClient.

### [UdpTerminalTool] - Infrastructure Utility
- Standalone executable launched in external terminals.
- Acts as network nodes for real-time monitoring and data echoing.

## 2. External Dependencies
- **DocumentFormat.OpenXml**: Essential for deep integration with Microsoft Office file structures.
- **Microsoft.Data.SqlClient**: Standard provider for high-performance SQL Server connectivity.
- **System.Text.Json**: Used for robust, lightweight settings persistence.

## 3. Communication Workflow
WPF App (Client) <--> UdpTerminalTool (Server) via TCP/UDP.
Logs are redirected to Monitor terminals for real-time traffic analysis.
