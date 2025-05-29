using System;
using System.Collections.Generic;

namespace gitSyncApp
{
    // This class provides actions that can be performed on a GitRepositoryStatus instance.
    public static class GitRepositoryStatusActions
    {
        // Default action: Analyzes the repository state and outputs a report to SafeConsoleWriter.Default.
        //
        // We use SafeConsoleWriter here because repositories are checked asynchronously,
        // and we want to display partially colored reports for each repository as soon as they are available.
        public static void AnalyzeAndReport(GitRepositoryStatus status)
        {
            var writer = SafeConsoleWriter.Default;
            var chunks = new List<ConsoleChunk>();
            bool anyActionRequired = false;

            // Write 'Repository: ' label, then repo name in white on blue, then the rest
            string repoLabel = "Repository: ";
            string repoName = status.Name;
            string repoPath = $" ({status.RepositoryPath}){Environment.NewLine}";
            chunks.Add(new ConsoleChunk(repoLabel));
            chunks.Add(new ConsoleChunk(repoName, ConsoleColor.White, ConsoleColor.Blue));
            chunks.Add(new ConsoleChunk(repoPath));

            // Check for missing branch info
            if (string.IsNullOrWhiteSpace(status.LocalBranch)) {
                chunks.Add(new ConsoleChunk($"Local branch is not set or could not be determined.{Environment.NewLine}", ConsoleColor.Red));
                writer.EnqueueChunks(chunks);
                return;
            }
            if (string.IsNullOrWhiteSpace(status.RemoteBranch)) {
                chunks.Add(new ConsoleChunk($"Remote branch is not set or could not be determined.{Environment.NewLine}", ConsoleColor.Red));
                writer.EnqueueChunks(chunks);
                return;
            }

            chunks.Add(new ConsoleChunk($"Local Branch: {status.LocalBranch}{Environment.NewLine}"));
            chunks.Add(new ConsoleChunk($"Remote Branch: {status.RemoteBranch}{Environment.NewLine}"));

            // Helper to add a list if it has items
            void AddList(string label, List<string> items, string prefix) {
                if (items.Count == 0) return;
                anyActionRequired = true;
                chunks.Add(new ConsoleChunk($"{label}: {items.Count}{Environment.NewLine}"));
                foreach (var item in items)
                    chunks.Add(new ConsoleChunk($"    {prefix}{item}{Environment.NewLine}", ConsoleColor.Yellow));
            }

            AddList("Untracked files", status.UntrackedFiles, "[untracked] ");
            AddList("Modified files", status.ModifiedFiles, "[modified] ");
            AddList("Deleted files", status.DeletedFiles, "[deleted] ");
            AddList("Staged files", status.StagedFiles, "[staged] ");
            AddList("Conflicted files", status.ConflictedFiles, "[conflicted] ");
            AddList("Stashed entries", status.StashedFiles, "[stashed] ");
            AddList("Unpushed commits", status.UnpushedCommits, "[unpushed] ");
            AddList("Unpulled commits", status.UnpulledCommits, "[unpulled] ");

            if (!anyActionRequired)
                return;

            writer.EnqueueChunks(chunks);
        }
    }
}
