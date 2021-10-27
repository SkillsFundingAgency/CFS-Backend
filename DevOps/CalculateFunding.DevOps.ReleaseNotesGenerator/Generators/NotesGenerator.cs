using CalculateFunding.DevOps.ReleaseNotesGenerator.Helpers;
using CalculateFunding.DevOps.ReleaseNotesGenerator.Models;
using CalculateFunding.DevOps.ReleaseNotesGenerator.Options;
using CalculateFunding.Services.Core.Extensions;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Wiki.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Generators
{
    public class NotesGenerator : INotesGenerator
    {
        private readonly ReleaseDefinitionOptions _releaseDefinitionOptions;
        private readonly Regex workItemIdRegex = new Regex(@"_apis/wit/workItems/(\d{1,7})");
        private readonly ILogger _logger;

        public NotesGenerator(
            IOptions<ReleaseDefinitionOptions> releaseDefinitionOptions,
            ILogger logger)
        {
            _releaseDefinitionOptions = releaseDefinitionOptions.Value;
            _logger = logger;
        }

        public async Task Generate(ConsoleOptions consoleOptions)
        {
            ReleaseNotesResult releaseNotesResult = await GenerateReleaseNoteResult(consoleOptions);
            string releaseNotesText = GenerateReleaseNotesText(releaseNotesResult, consoleOptions);

            if (consoleOptions.CreateWikiPage)
            {
                await GenerateReleaseNotesWikiPage(consoleOptions, releaseNotesText);
            }
        }

        private async Task<ReleaseNotesResult> GenerateReleaseNoteResult(ConsoleOptions consoleOptions)
        {
            try
            {
                _logger.Information("ReleaseNoteGenerator - GenerateReleaseNoteResult started.");

                ReleaseNotesResult releaseNotesResult = new ReleaseNotesResult
                {
                    SourcePhase = consoleOptions.SourceReleasePhase,
                    DestinationPhase = consoleOptions.DestinationReleasePhase
                };

                VssBasicCredential creds = new VssBasicCredential(string.Empty, consoleOptions.PAT);
                using VssConnection connection = new VssConnection(new Uri(consoleOptions.BaseURL), creds);

                using ReleaseHttpClient releaseClient = connection.GetClient<ReleaseHttpClient>();

                using WorkItemTrackingHttpClient workItemTrackingHttpClient = connection.GetClient<WorkItemTrackingHttpClient>();

                _logger.Information("Retrieving given Release Definitions");

                IEnumerable<ReleaseDefinition> releaseDefinitions 
                    = await GetRequiredReleaseDefinitions(releaseClient, consoleOptions.ProjectName);

                _logger.Information($"Retrieved {releaseDefinitions.Count()} release definitions");

                Dictionary<string, List<WorkItemRef>> linkedWorkItemsPerReleaseDefinition = new Dictionary<string, List<WorkItemRef>>();

                foreach (ReleaseDefinition releaseDefinition in releaseDefinitions)
                {
                    ReleaseDefinitionRef releaseDefinitionRef = ToReleaseDefinitionRef(releaseDefinition, consoleOptions.BaseURL, consoleOptions.ProjectName);

                    ReleaseContainer sourceReleaseContainer = new ReleaseContainer();
                    ReleaseContainer destinationReleaseContainer = new ReleaseContainer();

                    sourceReleaseContainer.ReleaseDefinition = releaseDefinitionRef;
                    destinationReleaseContainer.ReleaseDefinition = releaseDefinitionRef;

                    Release latestSourceRelease 
                        = await GetLatestSuccededReleaseId(releaseClient, consoleOptions.SourceReleasePhase, releaseDefinition, consoleOptions.ProjectName);
                    Release latestDestinationRelease 
                        = await GetLatestSuccededReleaseId(releaseClient, consoleOptions.DestinationReleasePhase, releaseDefinition, consoleOptions.ProjectName);

                    if (latestSourceRelease?.Id == null || latestDestinationRelease?.Id == null)
                    {
                        linkedWorkItemsPerReleaseDefinition.Add(releaseDefinition.Name, new List<WorkItemRef>());
                        continue;
                    }

                    ReleaseRef latestSourceReleaseRef = ToReleaseRef(latestSourceRelease, consoleOptions.BaseURL, consoleOptions.ProjectName);
                    ReleaseRef latestDestinationReleaseRef = ToReleaseRef(latestDestinationRelease, consoleOptions.BaseURL, consoleOptions.ProjectName);

                    _logger.Information(
                        $"ReleaseDefinition={releaseDefinitionRef.Name} for SourcePhase={consoleOptions.SourceReleasePhase} SourceReleaseName={latestSourceReleaseRef.Name} and " +
                        $"DestinationPhase={consoleOptions.DestinationReleasePhase} DestinationReleaseName={latestDestinationReleaseRef.Name}");

                    sourceReleaseContainer.Release = latestSourceReleaseRef;
                    destinationReleaseContainer.Release = latestDestinationReleaseRef;

                    List<ReleaseWorkItemRef> releaseWorkItemRefs =
                        await releaseClient.GetReleaseWorkItemsRefsAsync(consoleOptions.ProjectName, releaseId: latestSourceRelease.Id, baseReleaseId: latestDestinationRelease.Id);

                    IEnumerable<int> workItemRefIds = releaseWorkItemRefs.Select(_ => int.Parse(_.Id)).Where(_ => _ != 0);

                    _logger.Information(
                        $"ReleaseDefinition={releaseDefinitionRef.Name} for SourcePhase={consoleOptions.SourceReleasePhase} SourceReleaseName={latestSourceReleaseRef.Name} and " +
                        $"DestinationPhase={consoleOptions.DestinationReleasePhase} DestinationReleaseName={latestDestinationReleaseRef.Name} " +
                        $"has LinkedWorkItemCount={workItemRefIds.Count()} LinkedWorkItemIds={string.Join(',', workItemRefIds)}");

                    releaseNotesResult.SourceReleases.Add(sourceReleaseContainer);
                    releaseNotesResult.DestinationReleases.Add(destinationReleaseContainer);

                    if (!workItemRefIds.Any())
                    {
                        linkedWorkItemsPerReleaseDefinition.Add(releaseDefinition.Name, new List<WorkItemRef>());
                        continue;
                    }

                    List<WorkItem> workItems =
                            await workItemTrackingHttpClient.GetWorkItemsAsync(
                                consoleOptions.ProjectName,
                                workItemRefIds,
                                expand: WorkItemExpand.Relations);
                    List<WorkItemRef> workItemRefs = workItems.Select(_ => ToWorkItemRef(_, consoleOptions.BaseURL, consoleOptions.ProjectName)).ToList();

                    linkedWorkItemsPerReleaseDefinition.Add(releaseDefinition.Name, workItemRefs);
                }

                List<WorkItemRef> parentWorkItemRefs 
                    = await GetParentWorkItemRefs(linkedWorkItemsPerReleaseDefinition.SelectMany(_ => _.Value), workItemTrackingHttpClient, consoleOptions.ProjectName, consoleOptions.BaseURL);

                foreach (string relaseDefinitionName in linkedWorkItemsPerReleaseDefinition.Keys)
                {
                    List<WorkItemRef> workItemRefs = linkedWorkItemsPerReleaseDefinition[relaseDefinitionName];

                    foreach (WorkItemRef parentWorkItemRef in parentWorkItemRefs)
                    {
                        int index = workItemRefs.FindIndex(item => item.ParentWorkItemId == parentWorkItemRef.Id);
                        if (index != -1)
                        {
                            workItemRefs[index] = parentWorkItemRef;
                        }
                    }
                }

                releaseNotesResult.SourceReleases.ForEach(_ => _.LinkedWorkItems = linkedWorkItemsPerReleaseDefinition[_.ReleaseDefinition.Name].OrderBy(_ => _.Id));
                releaseNotesResult.WorkItemsToRelease = releaseNotesResult.SourceReleases.SelectMany(_ => _.LinkedWorkItems).Distinct().OrderBy(_ => _.Id);

                _logger.Information("ReleaseNoteGenerator - GenerateReleaseNoteResult completed.");

                return releaseNotesResult;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ReleaseNoteGenerator - GenerateReleaseNoteResult error");
                throw;
            }
        }

        private async Task GenerateReleaseNotesWikiPage(ConsoleOptions consoleOptions, string content)
        {
            try
            {
                _logger.Information("ReleaseNoteGenerator - GenerateReleaseNotesWikiPage started.");

                VssBasicCredential creds = new VssBasicCredential(string.Empty, consoleOptions.PAT);
                using VssConnection connection = new VssConnection(new Uri(consoleOptions.BaseURL), creds);

                using WikiHttpClient wikiClient = connection.GetClient<WikiHttpClient>();

                WikiPageCreateOrUpdateParameters wikiPageCreateOrUpdateParameters = new WikiPageCreateOrUpdateParameters
                {
                    Content = content
                };

                string wikiPath = $"{consoleOptions.WikiPath}-{DateTime.UtcNow:s}";

                WikiPageResponse wikiPageResponse = await wikiClient.CreateOrUpdatePageAsync(
                    wikiPageCreateOrUpdateParameters,
                    project: consoleOptions.ProjectName,
                    wikiIdentifier: consoleOptions.WikiIdentifier,
                    path: wikiPath,
                    Version: null,
                    comment: "Initial wiki page creation by CalculateFunding.DevOps.ReleaseNotesGenerator console application.");

                _logger.Information($"Wiki page created on WikiIdentifier={wikiPageResponse.Page.Path}");

                _logger.Information("ReleaseNoteGenerator - GenerateReleaseNotesWikiPage completed.");
            }
            catch (Exception ex)
            {
                string errorText = "Error while creating wiki page";
                _logger.Error(ex, errorText);

                throw;
            }
        }

        private string GenerateReleaseNotesText(
            ReleaseNotesResult releaseNotesResult,
            ConsoleOptions consoleOptions)
        {
            _logger.Information("ReleaseNoteGenerator - GenerateReleaseNotesText started.");

            string successReleaseNoteTemplatePath = Path.Join("Templates", "release-note-template-success.liquid");

            FluidParser parser = new FluidParser();
            string source = File.ReadAllText(successReleaseNoteTemplatePath);

            if (parser.TryParse(source, out IFluidTemplate template, out string error))
            {
                TemplateOptions options = new TemplateOptions();
                options.MemberAccessStrategy.Register<ReleaseNotesResult>();
                options.MemberAccessStrategy.Register<ReleaseContainer>();
                options.MemberAccessStrategy.Register<ReleaseDefinitionRef>();
                options.MemberAccessStrategy.Register<ReleaseRef>();
                options.MemberAccessStrategy.Register<WorkItemRef>();

                TemplateContext context = new TemplateContext(options).SetValue("result", new ObjectValue(releaseNotesResult));
                string renderedText = template.Render(context);

                File.WriteAllText(consoleOptions.ReleaseNoteFilePath, renderedText, Encoding.UTF8);

                _logger.Information("ReleaseNoteGenerator - GenerateReleaseNotesText completed.");

                return renderedText;
            }
            else
            {
                string errorText = $"Error while generating release notes. Error message: {error}";

                _logger.Error(errorText);

                throw new Exception(errorText);
            }

        }

        private async Task<List<WorkItemRef>> GetParentWorkItemRefs(
            IEnumerable<WorkItemRef> workItemRefs,
            WorkItemTrackingHttpClient workItemTrackingHttpClient,
            string projectName,
            string baseUrl)
        {
            IEnumerable<int> taskIds = workItemRefs.Where(_ => _.Type == "Task").Select(_ => _.Id).Distinct();

            List<WorkItem> tasks =
                await workItemTrackingHttpClient.GetWorkItemsAsync(
                    projectName,
                    taskIds,
                    expand: WorkItemExpand.Relations);

            IEnumerable<int> parentWorkItemIds = tasks.Select(_ => GetParentWorkItemID(_)).Where(_ => _ != null).Select(_ => _.GetValueOrDefault());

            List<WorkItem> parentWorkItems =
                await workItemTrackingHttpClient.GetWorkItemsAsync(
                    projectName,
                    ids: parentWorkItemIds);
            return parentWorkItems.Select(_ => ToWorkItemRef(_, baseUrl, projectName, parseParent: false)).ToList();
        }

        private async Task<IEnumerable<ReleaseDefinition>> GetRequiredReleaseDefinitions(
            ReleaseHttpClient releaseHttpClient,
            string projectName)
        {
            List<ReleaseDefinition> releaseDefinitions
                = await releaseHttpClient.GetReleaseDefinitionsAsync(projectName, expand: ReleaseDefinitionExpands.Environments);

            return releaseDefinitions.Where(_ => _releaseDefinitionOptions.ReleaseDefinitionNames.Contains(_.Name));
        }

        private async Task<Release> GetLatestSuccededReleaseId(
            ReleaseHttpClient releaseHttpClient,
            string releasePhaseName,
            ReleaseDefinition releaseDefinition,
            string projectName)
        {
            string[] environmentNames
                    = _releaseDefinitionOptions.ReleaseDefinitionStages.SingleOrDefault(_ => _.FriendlyName == releasePhaseName).EnvironmentNames;

            ReleaseDefinitionEnvironment environment = releaseDefinition.Environments.FirstOrDefault(_ => environmentNames.Select(_ => _.ToLowerInvariant()).Contains(_.Name.ToLowerInvariant()));

            if(environment == null)
            {
                return null;
            }

            var sourceReleases = await releaseHttpClient.GetReleasesAsync(
                projectName,
                definitionId: releaseDefinition.Id,
                definitionEnvironmentId: environment.Id,
                environmentStatusFilter: EnvironmentStatus.Succeeded.GetHashCode(),
                top: 1,
                queryOrder: ReleaseQueryOrder.Descending);
            return sourceReleases.FirstOrDefault();
        }

        private WorkItemRef ToWorkItemRef(
            WorkItem workItem,
            string adoBaseUrl,
            string projectName,
            bool parseParent = true)
            => new WorkItemRef
            {
                Id = workItem.Id.GetValueOrDefault(),
                URL = $"{adoBaseUrl}/{projectName}/_workItems/edit/{workItem.Id.GetValueOrDefault()}".EncodeURL(),
                State = workItem.Fields["System.State"] as string,
                Title = workItem.Fields["System.Title"] as string,
                Type = workItem.Fields["System.WorkItemType"] as string,
                ParentWorkItemId = parseParent ? GetParentWorkItemID(workItem) : (int?) null
            };

        private static string GetParentWorkItemUrl(WorkItem workItem)
            => workItem.Relations.SingleOrDefault(_ => _.Rel == "System.LinkTypes.Hierarchy-Reverse")?.Url;

        private int? GetParentWorkItemID(WorkItem workItem)
        {
            string parentUrl = GetParentWorkItemUrl(workItem);

            if (string.IsNullOrWhiteSpace(parentUrl))
            {
                return null;
            }

            Match match = workItemIdRegex.Match(parentUrl);
            if (!match.Success)
            {
                return null;
            }

            bool parsed = int.TryParse(match.Groups[1].Value, out int parentWorkItemId);
            return parsed ? parentWorkItemId : (int?) null;
        }

        private ReleaseDefinitionRef ToReleaseDefinitionRef(
            ReleaseDefinition releaseDefinition,
            string adoBaseUrl,
            string projectName)
            => new ReleaseDefinitionRef
            {
                Id = releaseDefinition.Id,
                Name = releaseDefinition.Name,
                Url = $"{adoBaseUrl}/{projectName}/_release?definitionId={releaseDefinition.Id}&view=all&_a=releases".EncodeURL()
            };

        private ReleaseRef ToReleaseRef(
            Release release,
            string adoBaseUrl,
            string projectName)
            => new ReleaseRef
            {
                Id = release.Id,
                Name = release.Name,
                Url = $"{adoBaseUrl}/{projectName}/_releaseProgress?_a=release-pipeline-progress&releaseId={release.Id}".EncodeURL()
            };
    }
}
