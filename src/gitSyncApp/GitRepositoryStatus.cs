using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace gitSyncApp
{
    public class GitRepositoryStatus
    {
        public string RepositoryPath { get; }
        public string Name { get; }
        public List<string> UntrackedFiles { get; private set; }
        public List<string> ModifiedFiles { get; private set; }
        public List<string> DeletedFiles { get; private set; }
        public List<string> StagedFiles { get; private set; }
        public List<string> ConflictedFiles { get; private set; }
        public List<string> StashedFiles { get; private set; }
        public List<string> UnpushedCommits { get; private set; }
        public List<string> UnpulledCommits { get; private set; }
        public string LocalBranch { get; private set; }
        public string RemoteBranch { get; private set; }

        public GitRepositoryStatus(string repoPath)
        {
            RepositoryPath = repoPath;
            Name = Path.GetFileName(repoPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            UntrackedFiles = new List<string>();
            ModifiedFiles = new List<string>();
            DeletedFiles = new List<string>();
            StagedFiles = new List<string>();
            ConflictedFiles = new List<string>();
            StashedFiles = new List<string>();
            UnpushedCommits = new List<string>();
            UnpulledCommits = new List<string>();
            LocalBranch = string.Empty;
            RemoteBranch = string.Empty;
        }

        // Helper method to split text into lines reliably (handles all line endings)
        private static IEnumerable<string> ReadLines(string text)
        {
            using var reader = new StringReader(text);
            string? line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }

        private async Task<string> RunGitCommandAsync(string arguments)
        {
            var psi = new ProcessStartInfo(GitLocator.Default, arguments)
            {
                WorkingDirectory = RepositoryPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException($"Failed to start git process: git {arguments}");
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            return output;
        }

        public async Task RefreshStatusAsync(Action<GitRepositoryStatus>? onRefreshed = null)
        {
            await ParseStatusAsync();
            await GetStashedFilesAsync();
            await GetBranchesAsync();
            await GetUnpushedCommitsAsync();
            await GetUnpulledCommitsAsync();
            onRefreshed?.Invoke(this);
        }

        // Parses the output of 'git status --porcelain' to update file status lists.
        // GIT COMMAND: 'git status --porcelain'
        //   - This command provides a machine-readable summary of the current working directory and staging area.
        //   - Each line represents a file and its status (e.g., modified, staged, untracked, conflicted, deleted).
        //   - Used here to fill UntrackedFiles, ModifiedFiles, DeletedFiles, StagedFiles, and ConflictedFiles lists.
        // Recognized attributes:
        //   - Untracked: ??
        //   - Conflicted: U, AA, DD
        //   - Modified: M, T, R, C (in the second column)
        //   - Deleted: D (in the second column)
        //   - Staged: M, T, A, D, R, C (in the first column)
        private async Task ParseStatusAsync()
        {
            UntrackedFiles.Clear();
            ModifiedFiles.Clear();
            DeletedFiles.Clear();
            StagedFiles.Clear();
            ConflictedFiles.Clear();
            var output = await RunGitCommandAsync("status --porcelain");
            foreach (var line in ReadLines(output))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var x = line[0];
                var y = line[1];
                var file = line.Substring(3);
                if (x == '?' && y == '?') UntrackedFiles.Add(file);
                else if (x == 'U' || y == 'U' || (x == 'A' && y == 'A') || (x == 'D' && y == 'D')) ConflictedFiles.Add(file);
                else if ("MTRC".Contains(y)) ModifiedFiles.Add(file);
                else if (y == 'D') DeletedFiles.Add(file);
                else if ("MTADRC".Contains(x)) StagedFiles.Add(file);
            }
        }

        // GIT COMMAND: 'git stash list'
        //   - Lists all stashed changes in the repository.
        //   - Each line represents a stash entry (temporary saved changes not yet committed).
        //   - Used here to fill the StashedFiles list.
        private async Task GetStashedFilesAsync()
        {
            StashedFiles.Clear();
            var output = await RunGitCommandAsync("stash list");
            foreach (var line in ReadLines(output))
            {
                if (!string.IsNullOrWhiteSpace(line)) StashedFiles.Add(line);
            }
        }

        // GIT COMMANDS:
        //   1. 'git rev-parse --abbrev-ref HEAD'
        //      - Returns the name of the current local branch (e.g., 'main' or 'feature/xyz').
        //   2. 'git rev-parse --abbrev-ref --symbolic-full-name HEAD@{upstream}'
        //      - Returns the name of the remote branch this local branch is tracking (e.g., 'origin/main').
        //      - If not tracking any remote branch, this may be empty or error.
        //   - Used here to set LocalBranch and RemoteBranch properties.
        private async Task GetBranchesAsync()
        {
            LocalBranch = (await RunGitCommandAsync("rev-parse --abbrev-ref HEAD")).Trim();
            RemoteBranch = (await RunGitCommandAsync("rev-parse --abbrev-ref --symbolic-full-name HEAD@{upstream}")).Trim();
        }

        // GIT COMMAND: 'git log {RemoteBranch}..HEAD --oneline'
        //   - Lists commits that exist in the local branch but not yet pushed to the remote branch.
        //   - Each line is a commit (short hash and message).
        //   - Used here to fill the UnpushedCommits list.
        private async Task GetUnpushedCommitsAsync()
        {
            UnpushedCommits.Clear();
            if (string.IsNullOrWhiteSpace(RemoteBranch)) return;
            var output = await RunGitCommandAsync($"log {RemoteBranch}..HEAD --oneline");
            foreach (var line in ReadLines(output))
            {
                if (!string.IsNullOrWhiteSpace(line)) UnpushedCommits.Add(line);
            }
        }

        // GIT COMMANDS:
        //   1. 'git fetch {remoteName}'
        //      - Updates local information about the remote repository (fetches new commits, branches, etc.).
        //      - Does not modify working files or branches, just updates metadata.
        //   2. 'git log HEAD..{RemoteBranch} --oneline'
        //      - Lists commits that exist in the remote branch but not yet pulled into the local branch.
        //      - Each line is a commit (short hash and message).
        //      - Used here to fill the UnpulledCommits list.
        private async Task GetUnpulledCommitsAsync()
        {
            UnpulledCommits.Clear();
            if (string.IsNullOrWhiteSpace(RemoteBranch)) return;
            var remoteName = RemoteBranch.Split('/')[0];
            await RunGitCommandAsync($"fetch {remoteName}");
            var output = await RunGitCommandAsync($"log HEAD..{RemoteBranch} --oneline");
            foreach (var line in ReadLines(output))
            {
                if (!string.IsNullOrWhiteSpace(line)) UnpulledCommits.Add(line);
            }
        }

        public bool HasRemoteUpdates()
        {
            // Returns true if there are commits in the remote branch not yet pulled
            return UnpulledCommits.Count > 0;
        }

        public bool IsSafeToPull()
        {
            // Ensure both branches are set
            if (string.IsNullOrWhiteSpace(LocalBranch) || string.IsNullOrWhiteSpace(RemoteBranch))
                return false;

            // Safe to pull if:
            // - No untracked, modified, deleted, staged, or conflicted files
            // - No stashed changes
            // - No unpushed commits (no local/remote divergence)
            return UntrackedFiles.Count == 0
                && ModifiedFiles.Count == 0
                && DeletedFiles.Count == 0
                && StagedFiles.Count == 0
                && ConflictedFiles.Count == 0
                && StashedFiles.Count == 0
                && UnpushedCommits.Count == 0;
        }

        // Pulls commits from the remote branch and returns the output
        public async Task<string> PullCommitsAsync()
        {
            if (string.IsNullOrWhiteSpace(RemoteBranch))
                throw new InvalidOperationException("No remote branch is set for this repository.");
            var remoteName = RemoteBranch.Split('/')[0];
            var branchName = RemoteBranch.Substring(remoteName.Length + 1); // after the first '/'
            return await RunGitCommandAsync($"pull {remoteName} {branchName}");
        }
    }
}
