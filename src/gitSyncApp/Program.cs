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
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No root directories are specified in settings.");
                    Console.ResetColor();
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
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No git repositories found in the specified root directories.");
                    Console.ResetColor();
                    return;
                }

                // Analyze all repositories in parallel
                var repoStatuses = new List<GitRepositoryStatus>();
                var tasks = new List<Task>();
                foreach (var repoPath in repoPaths)
                {
                    var status = new GitRepositoryStatus(repoPath);
                    repoStatuses.Add(status);
                    tasks.Add(Task.Run(async () => {
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

                // List safe-to-pull repositories
                var safeToPull = repoStatuses.Where(r => r.IsSafeToPull()).ToList();
                if (safeToPull.Count > 0)
                {
                    Console.WriteLine("Repositories safe to pull:");
                    foreach (var repo in safeToPull)
                    {
                        Console.WriteLine($"    {repo.Name} ({repo.RepositoryPath})");
                    }
                    string answer = string.Empty;
                    while (answer != "y" && answer != "n")
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.Write("Do you want to pull these repositories? (y/n): ");
                        Console.ResetColor();
                        answer = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
                    }
                    if (answer == "y")
                    {
                        foreach (var repo in safeToPull)
                        {
                            try
                            {
                                var output = await repo.PullCommitsAsync();
                                Console.WriteLine($"Pulled {repo.Name}:");
                                Console.WriteLine(output.TrimEnd());
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Error pulling {repo.Name}: {ex}");
                                Console.ResetColor();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex}");
                Console.ResetColor();
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
