using CalculateFunding.Models.External;
using CalculateFunding.Models.External.AtomItems;
using CalculateFunding.Models.External.V4;
using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ExternalApiFeedWriter : IExternalApiFeedWriter
    {
        public async Task OutputFeedHeader(SearchFeedResult<ExternalFeedFundingGroupItem> searchFeed,
                                           string fundingUrl,
                                           PipeWriter writer)
        {
            AtomLink[] atomLinks = searchFeed.GenerateAtomLinksForResultGivenBaseUrl(fundingUrl).ToArray();

            await writer.WriteAsync("{");
            await writer.WriteAsync($"    \"id\":\"{Guid.NewGuid():N}\",");
            await writer.WriteAsync("    \"title\":\"Calculate Funding Service Funding Feed\",");
            await writer.WriteAsync("    \"author\":{");
            await writer.WriteAsync("                 \"name\":\"Calculate Funding Service\",");
            await writer.WriteAsync("                 \"email\":\"calculate-funding@education.gov.uk\"");
            await writer.WriteAsync("               },");
            await writer.WriteAsync($"    \"updated\":{JsonSerializer.Serialize(DateTimeOffset.Now)},");
            await writer.WriteAsync("    \"rights\":\"calculate-funding@education.gov.uk\",");
            await writer.WriteAsync("    \"link\": [");

            int linkCount = 0;

            foreach (AtomLink link in atomLinks)
            {
                linkCount++;
                await writer.WriteAsync("    {");
                await writer.WriteAsync($"        \"href\":\"{link.Href}\",");
                await writer.WriteAsync($"        \"rel\":\"{link.Rel}\"");
                await writer.WriteAsync("    }");

                if (linkCount != atomLinks.Length)
                {
                    await writer.WriteAsync(",");
                }
            }

            await writer.WriteAsync("             ],");
            await writer.WriteAsync("    \"atomEntry\": [");
        }

        public async Task OutputFeedItem(PipeWriter writer,
                                         string link,
                                         ExternalFeedFundingGroupItem feedItem,
                                         Stream contents,
                                         bool hasMoreItems)
        {
            string now = JsonSerializer.Serialize(feedItem.StatusChangedDate);

            await writer.WriteAsync("        {");
            await writer.WriteAsync($"             \"id\":\"{link}\",");
            await writer.WriteAsync($"             \"title\":\"{feedItem.FundingId}\",");
            await writer.WriteAsync($"             \"summary\":\"{feedItem.FundingId}\",");
            await writer.WriteAsync($"             \"published\": {now},");
            await writer.WriteAsync($"             \"updated\":{now},");
            await writer.WriteAsync($"             \"version\":\"{feedItem.MajorVersion}\",");
            await writer.WriteAsync("             \"link\":");
            await writer.WriteAsync("                     {");
            await writer.WriteAsync($"                         \"href\":\"{link}\",");
            await writer.WriteAsync("                         \"rel\":\"Funding\"");
            await writer.WriteAsync("                     },");
            await writer.WriteAsync("             \"content\":");

            byte[] buffer = new byte[262144];

            while (true)
            {
                int bytes = await contents.ReadAsync(buffer);
                if (bytes <= 0)
                {
                    break;
                }

                if (bytes < buffer.Length)
                {
                    buffer = buffer.Take(bytes).ToArray();
                }

                await writer.WriteAsync(buffer);
            }

            await writer.WriteAsync("        }");

            if (hasMoreItems)
            {
                await writer.WriteAsync(",");
            }
        }

        public async Task OutputFeedFooter(PipeWriter writer)
        {
            await writer.WriteAsync("]}");
        }
    }
}
