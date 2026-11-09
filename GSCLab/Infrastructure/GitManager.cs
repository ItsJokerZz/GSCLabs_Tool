
using LibGit2Sharp;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace GSCLab.Infrastructure
{
    public class GitManager
    {
        private string _username;
        private string _personalAccessToken;

        public GitManager(string username, string token)
        {
            _username = username;
            _personalAccessToken = token;
        }

        public static async Task<GitManager> InitializeAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient("45.90.116.64", 8888))
                using (NetworkStream stream = client.GetStream())
                {
                    string cmd = $"GET_GITHUB_CREDS|{Infrastructure.Session.Token}";
                    byte[] data = Encoding.UTF8.GetBytes(cmd);
                    await stream.WriteAsync(data, 0, data.Length);

                    byte[] buffer = new byte[8192];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response.StartsWith("GITHUB_CREDS_SUCCESS"))
                    {
                        string[] parts = response.Split('|');
                        if (parts.Length >= 3)
                        {
                            return new GitManager(parts[1], parts[2]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to fetch Git Creds: " + ex.Message);
            }

            return null;
        }

        private Credentials CreateCredentials(string url, string usernameFromUrl, SupportedCredentialTypes types)
        {
            return new UsernamePasswordCredentials
            {
                Username = _username,
                Password = _personalAccessToken
            };
        }

        public bool CloneRepository(string remoteUrl, string localPath, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                var options = new CloneOptions();

                options.FetchOptions.CredentialsProvider = CreateCredentials;

                Repository.Clone(remoteUrl, localPath, options);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool PullChanges(string localRepoPath, string userEmail, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (var repo = new Repository(localRepoPath))
                {
                    var options = new PullOptions
                    {
                        FetchOptions = new FetchOptions { CredentialsProvider = CreateCredentials }
                    };

                    var signature = new Signature(_username, userEmail, DateTimeOffset.Now);

                    Commands.Pull(repo, signature, options);
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool CommitAndPush(string localRepoPath, string commitMessage, string userEmail, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (var repo = new Repository(localRepoPath))
                {
                    Commands.Stage(repo, "*");

                    var signature = new Signature(_username, userEmail, DateTimeOffset.Now);
                    repo.Commit(commitMessage, signature, signature);

                    var remote = repo.Network.Remotes["origin"];
                    var options = new PushOptions { CredentialsProvider = CreateCredentials };

                    repo.Network.Push(remote, @"refs/heads/main", options);
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
