using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace gitSyncApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Scan for git repositories
                var rootDirs = AppSettings.GetRepositoryRootDirectories().ToList();
                if (rootDirs.Count == 0)
                {
                    ConsoleColorUtil.WriteColoredLine("No root directories are specified in settings.", ConsoleColor.Yellow);
                    return;
                }
                var ignorePaths = new HashSet<string>(AppSettings.GetIgnoreDirectoryPaths(), StringComparer.OrdinalIgnoreCase);
                var ignoreNames = new HashSet<string>(AppSettings.GetIgnoreDirectoryNames(), StringComparer.OrdinalIgnoreCase);
                var repoPaths = new List<string>();

                void ScanDir(string dir)
                {
                    if (ignorePaths.Contains(dir)) return;
                    var dirName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (ignoreNames.Contains(dirName)) return;
                    if (Directory.Exists(Path.Combine(dir, ".git")))
                    {
                        repoPaths.Add(dir);
                        return; // Don't scan subdirs of a repo
                    }
                    foreach (var sub in Directory.GetDirectories(dir))
                    {
                        ScanDir(sub);
                    }
                }
                foreach (var root in rootDirs)
                {
                    if (Directory.Exists(root))
                        ScanDir(root);
                }
                repoPaths = repoPaths.Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                if (repoPaths.Count == 0)
                {
                    ConsoleColorUtil.WriteColoredLine("No git repositories found in the specified root directories.", ConsoleColor.Yellow);
                    return;
                }

                // Analyze all repositories in parallel
                var repoStatuses = new List<GitRepositoryStatus>();
                var tasks = new List<Task>();
                foreach (var repoPath in repoPaths)
                {
                    var status = new GitRepositoryStatus(repoPath);
                    repoStatuses.Add(status);
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await status.RefreshStatusAsync(GitRepositoryStatusActions.AnalyzeAndReport);
                        }
                        catch (Exception ex)
                        {
                            SafeConsoleWriter.Default.EnqueueChunks(new[] {
                                new ConsoleChunk($"Failed to check {repoPath}: {ex}{Environment.NewLine}", ConsoleColor.Red)
                            });
                        }
                    }));
                }
                await Task.WhenAll(tasks);

                // If all repositories are up to date, display a message and return
                if (repoStatuses.All(r => r.IsUpToDate()))
                {
                    ConsoleColorUtil.WriteColoredLine("All repositories are up to date.", ConsoleColor.Green);
                    return;
                }

                // List repositories that have remote updates and are safe to pull
                var remoteUpdatesAndSafe = repoStatuses.Where(r => r.HasRemoteUpdates() && r.IsSafeToPull()).ToList();
                if (remoteUpdatesAndSafe.Count == 0)
                {
                    Console.WriteLine("No repositories have remote updates and are safe to pull.");
                }
                else
                {
                    Console.WriteLine("Repositories that have remote updates and are safe to pull:");
                    foreach (var repo in remoteUpdatesAndSafe)
                    {
                        Console.WriteLine($"    {repo.Name} ({repo.RepositoryPath})");
                    }
                    string answer = string.Empty;
                    while (answer != "y" && answer != "n")
                    {
                        ConsoleColorUtil.WriteColored("Do you want to pull these repositories? (y/n): ", ConsoleColor.White, ConsoleColor.Blue);
                        answer = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
                    }
                    if (answer == "y")
                    {
                        foreach (var repo in remoteUpdatesAndSafe)
                        {
                            try
                            {
                                var output = await repo.PullCommitsAsync();
                                Console.WriteLine($"Pulled {repo.Name}:");
                                Console.WriteLine(output.Trim());
                            }
                            catch (Exception ex)
                            {
                                ConsoleColorUtil.WriteColoredLine($"Error pulling {repo.Name}: {ex}", ConsoleColor.Red);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleColorUtil.WriteColoredLine($"Error: {ex}", ConsoleColor.Red);
            }
            finally
            {
                // Ensure all enqueued console output is written before prompting the user to exit.
                SafeConsoleWriter.Default.Dispose();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
