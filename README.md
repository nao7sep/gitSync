# gitSync

gitSync is a cross-platform console application for automated monitoring and safe synchronization of multiple Git repositories. It scans specified directories, analyzes repository status, and enables safe, automated pulling of remote updates.

## Key Features

- Recursively discovers Git repositories in configured root directories
- Analyzes repository status (untracked, modified, staged, deleted, conflicted files)
- Detects local/remote branch and commit status (unpushed/unpulled commits, stashed changes)
- Identifies repositories that are safe to pull automatically (no uncommitted or stashed changes, no unpushed commits)
- Presents a summary and detailed status reports for all repositories
- Prompts for confirmation before performing pull operations
- Provides colored console output for clarity
- Runs repository analysis in parallel for performance
- Cross-platform support: Windows, macOS, Linux

## Usage

1. Configure root directories and ignore patterns in `appsettings.json`.
2. Run gitSync from the console.
3. Review the status summary and follow prompts to safely synchronize repositories.

---
For detailed technical specifications and architecture, see [`specs/gitsync-specifications-v0.1.md`](specs/gitsync-specifications-v0.1.md).