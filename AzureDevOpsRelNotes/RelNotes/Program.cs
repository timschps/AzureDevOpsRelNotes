using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
// https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

// https://www.nuget.org/packages/Microsoft.VisualStudio.Services.InteractiveClient/
using Microsoft.VisualStudio.Services.Client;

// https://www.nuget.org/packages/Microsoft.VisualStudio.Services.Client/
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace RelNotes
{
    class Program
    {
        public const string PAT_ENV_VAR = "AZURE_DEVOPS_PAT";


        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            VssConnection connection = GetVssConnection();

            ProjectHttpClient projectClient = connection.GetClient<ProjectHttpClient>();
            // Call to get the list of projects
            //IEnumerable<TeamProjectReference> projects = projectClient.GetProjects().Result;
            TeamProjectReference project = projectClient.GetProject("MITHRA").Result;


            GitHttpClient gitClient = connection.GetClient<GitHttpClient>();
            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();


            var repos = gitClient.GetRepositoriesAsync(project.Id, includeLinks: true).Result;
            var pullRequestSearchCriteria = new GitPullRequestSearchCriteria
            {
                Status = PullRequestStatus.Completed,
                IncludeLinks = true,



            };
            List<GitPullRequest> allPullRequests = new List<GitPullRequest>();
            int skip = 0;
            int threshold = 1000;

            foreach (var repo in repos)
            {
                Console.WriteLine($"{repo.Name} (DefaultBranch: {repo.DefaultBranch})");
                IList<GitPullRequest> prs = gitClient.GetPullRequestsAsync(repo.Id, pullRequestSearchCriteria, skip: skip, top: threshold
                    ).Result;
                foreach (GitPullRequest pr in prs.Where(x => x.Repository.Id == repo.Id))
                {
                    Console.WriteLine("\t{0} #{1} {2} -> {3} ({4})",
                        pr.Title.Substring(0, Math.Min(40, pr.Title.Length)),
                        pr.PullRequestId,
                        pr.SourceRefName,
                        pr.TargetRefName,
                        pr.CreatedBy.DisplayName);

                    var prlinks = gitClient.GetPullRequestWorkItemRefsAsync(project.Id, repo.Id, pr.PullRequestId).Result;
                    if (prlinks != null)
                    {
                        var wis = prlinks.ToList();
                        foreach (var wi in wis)
                        {
                            
                            Console.WriteLine($"\t\t{wi.Id}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\t\tNo links found");
                    }

                }
            }



            // Create instance of WorkItemTrackingHttpClient using VssConnection

            
        }

        private static VssConnection GetVssConnection()
        {
            string pat = Environment.GetEnvironmentVariable(Program.PAT_ENV_VAR);
            if (string.IsNullOrEmpty(pat))
            {
                throw new ArgumentException("On .NET Core, you must set an environment variable " + Program.PAT_ENV_VAR + " with a personal access token.");
            }

            var creds = new VssBasicCredential("pat", pat);

            VssConnection connection = new VssConnection(new Uri("https://fluxys.visualstudio.com"), creds);
            return connection;
        }
    }
}
