# gitSync Specifications v0.1

## Overview

**gitSync** is a cross-platform console application written in C# (.NET 9.0) that provides automated monitoring and synchronization capabilities for multiple Git repositories. The application scans specified directories for Git repositories, analyzes their status, and offers safe automated pulling of remote updates.

## Project Information

- **Product Name**: gitSync
- **Version**: 0.1
- **Target Framework**: .NET 9.0
- **License**: GPL-3.0
- **Author**: nao7sep
- **Company**: Purrfect Code
- **Repository**: https://github.com/nao7sep/gitSync

## Core Features

### 1. Repository Discovery
- Recursively scans configured root directories for Git repositories
- Identifies repositories by the presence of `.git` directories
- Supports configurable ignore patterns for directories and paths
- Cross-platform path handling (Windows, macOS, Linux)

### 2. Git Status Analysis
The application performs comprehensive status analysis for each repository:

- **File Status Tracking**:
  - Untracked files (`??`)
  - Modified files (`M`, `T`, `R`, `C` in working directory)
  - Deleted files (`D` in working directory)
  - Staged files (`M`, `T`, `A`, `D`, `R`, `C` in staging area)
  - Conflicted files (`U`, `AA`, `DD`)

- **Branch Information**:
  - Local branch name
  - Remote tracking branch name
  - Branch relationship validation

- **Commit Status**:
  - Unpushed commits (local commits not on remote)
  - Unpulled commits (remote commits not in local)
  - Stashed changes

### 3. Safe Synchronization
- Identifies repositories that are safe to pull automatically
- Safety criteria:
  - No uncommitted changes (untracked, modified, deleted, staged, conflicted files)
  - No stashed changes
  - No unpushed commits (prevents merge conflicts)
  - Valid local and remote branch configuration

### 4. Interactive Operations
- Displays summary of all repositories to be checked
- Shows detailed status reports for repositories requiring attention
- Prompts user for confirmation before performing pull operations
- Provides colored console output for better readability

## Architecture

### Core Classes

#### [`Program`](src/gitSyncApp/Program.cs:9)
Main entry point that orchestrates the entire workflow:
1. Loads configuration settings
2. Scans for Git repositories
3. Analyzes repositories in parallel
4. Presents results and handles user interaction

#### [`GitRepositoryStatus`](src/gitSyncApp/GitRepositoryStatus.cs:9)
Core class representing the state of a Git repository:
- **Properties**: Repository path, name, file status lists, branch information, commit lists
- **Methods**:
  - [`RefreshStatusAsync()`](src/gitSyncApp/GitRepositoryStatus.cs:70) - Updates all status information
  - [`IsUpToDate()`](src/gitSyncApp/GitRepositoryStatus.cs:190) - Checks if repository needs no action
  - [`HasRemoteUpdates()`](src/gitSyncApp/GitRepositoryStatus.cs:211) - Checks for unpulled commits
  - [`IsSafeToPull()`](src/gitSyncApp/GitRepositoryStatus.cs:217) - Validates pull safety
  - [`PullCommitsAsync()`](src/gitSyncApp/GitRepositoryStatus.cs:237) - Executes git pull

#### [`GitRepositoryStatusActions`](src/gitSyncApp/GitRepositoryStatusActions.cs:7)
Provides reporting actions for repository status:
- [`AnalyzeAndReport()`](src/gitSyncApp/GitRepositoryStatusActions.cs:13) - Generates colored console reports

#### [`AppSettings`](src/gitSyncApp/AppSettings.cs:7)
Configuration management using Microsoft.Extensions.Configuration:
- Loads settings from [`appsettings.json`](src/gitSyncApp/appsettings.json:1)
- Provides typed access to configuration sections

#### [`GitLocator`](src/gitSyncApp/GitLocator.cs:8)
Cross-platform Git executable discovery:
- Checks custom paths from configuration
- Falls back to platform-specific default locations
- Caches result for performance

### Utility Classes

#### [`SafeConsoleWriter`](src/gitSyncApp/SafeConsoleWriter.cs:21)
Thread-safe console output for parallel operations:
- Uses [`BlockingCollection`](src/gitSyncApp/SafeConsoleWriter.cs:25) for thread-safe queuing
- Background thread processes output chunks
- Supports colored text with [`ConsoleChunk`](src/gitSyncApp/SafeConsoleWriter.cs:8)

#### [`ConsoleColorUtil`](src/gitSyncApp/ConsoleColorUtil.cs:5)
Simple utility for colored console output:
- [`WriteColored()`](src/gitSyncApp/ConsoleColorUtil.cs:7) - Writes colored text
- [`WriteColoredLine()`](src/gitSyncApp/ConsoleColorUtil.cs:18) - Writes colored line

#### [`PathUtil`](src/gitSyncApp/PathUtil.cs:6)
Path manipulation utilities:
- [`GetRepoNameFromPath()`](src/gitSyncApp/PathUtil.cs:8) - Extracts repository name from path

## Configuration

### Configuration File: [`appsettings.json`](src/gitSyncApp/appsettings.json:1)

```json
{
    "Git": {
        "PossiblePaths": [
            "C:\\Custom\\Path\\To\\Git\\git.exe",
            "/usr/local/custom/git"
        ]
    },
    "RepositoryScan": {
        "RootDirectories": [
            "C:\\Repositories"
        ],
        "IgnoreDirectoryPaths": [],
        "IgnoreDirectoryNames": [
            ".git", ".svn", ".vs", ".vscode", "bin", "obj"
        ]
    }
}
```

### Configuration Sections

#### Git Configuration
- **PossiblePaths**: Custom paths to Git executable (checked before default locations)

#### Repository Scan Configuration
- **RootDirectories**: Base directories to scan for Git repositories
- **IgnoreDirectoryPaths**: Specific full paths to exclude from scanning
- **IgnoreDirectoryNames**: Directory names to exclude (e.g., build artifacts, IDE folders)

## Git Commands Used

The application uses the following Git commands for status analysis:

### Status Analysis
- `git status --porcelain` - Machine-readable working directory status
- `git stash list` - List stashed changes
- `git rev-parse --abbrev-ref HEAD` - Get current local branch name
- `git rev-parse --abbrev-ref --symbolic-full-name HEAD@{upstream}` - Get remote tracking branch

### Commit Analysis
- `git log {remote}..HEAD --oneline` - List unpushed commits
- `git fetch {remote}` - Update remote tracking information
- `git log HEAD..{remote} --oneline` - List unpulled commits

### Synchronization
- `git pull {remote} {branch}` - Pull and merge remote changes

## Workflow

### 1. Initialization
1. Load configuration from [`appsettings.json`](src/gitSyncApp/appsettings.json:1)
2. Validate root directories exist
3. Locate Git executable

### 2. Repository Discovery
1. Recursively scan root directories
2. Apply ignore filters (paths and names)
3. Identify Git repositories by `.git` directory presence
4. Deduplicate and sort repository list

### 3. Parallel Analysis
1. Create [`GitRepositoryStatus`](src/gitSyncApp/GitRepositoryStatus.cs:9) instances
2. Execute status analysis in parallel using [`Task.Run()`](src/gitSyncApp/Program.cs:68)
3. Generate reports using [`SafeConsoleWriter`](src/gitSyncApp/SafeConsoleWriter.cs:21)

### 4. Result Processing
1. Check if all repositories are up-to-date
2. Identify repositories with remote updates that are safe to pull
3. Present options to user
4. Execute pull operations if confirmed

### 5. Cleanup
1. Ensure all console output is flushed
2. Wait for user input before exit

## Error Handling

- Git command failures are captured and reported with colored output
- Repository analysis errors don't stop processing of other repositories
- Missing Git executable throws clear error message
- Invalid configuration values are handled gracefully

## Dependencies

### NuGet Packages
- **Microsoft.Extensions.Configuration** (9.0.0) - Configuration framework
- **Microsoft.Extensions.Configuration.Binder** (9.0.0) - Configuration binding
- **Microsoft.Extensions.Configuration.Json** (9.0.0) - JSON configuration provider

### System Requirements
- .NET 9.0 Runtime
- Git executable (automatically located or configured)
- Read access to repository directories
- Write access for Git operations (pull)

## Platform Support

The application is designed for cross-platform operation:
- **Windows**: Supports standard Git for Windows installations
- **macOS**: Supports Homebrew and system Git installations
- **Linux**: Supports standard package manager Git installations

## Security Considerations

- No credentials are stored or managed by the application
- Relies on existing Git credential configuration
- Only performs read operations and safe pull operations
- Validates repository safety before any write operations

## Performance Characteristics

- **Parallel Processing**: Repository analysis runs concurrently
- **Efficient Git Operations**: Minimal Git commands for maximum information
- **Memory Efficient**: Streams Git command output
- **Caching**: Git executable path is cached after first discovery

## Future Enhancements

Potential areas for future development:
- Push operation support
- Branch switching capabilities
- Merge conflict resolution assistance
- Configuration file validation
- Logging and audit trail
- GUI interface option
- Repository grouping and filtering
- Custom Git command execution

---
*Document version: v0.1*
