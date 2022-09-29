using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ChannelUrlToChannelResolver : IChannelUrlToChannelResolver
    {
        private readonly IReleaseManagementRepository _repo;

        private Dictionary<string, Channel?> _keyCache = new Dictionary<string, Channel?>();

        public ChannelUrlToChannelResolver(IReleaseManagementRepository releaseManagementRepository)
        {
            _repo = releaseManagementRepository;
        }

        public async Task<Channel> ResolveUrlToChannel(string urlKey)
        {
            string normalisedKey = urlKey.ToLowerInvariant();

            if (_keyCache.TryGetValue(normalisedKey, out Channel result))
            {
                return result;
            }

            result = await _repo.GetChannelFromUrlKey(normalisedKey);

            _keyCache.Add(normalisedKey, result);

            return result;
        }

        public async Task<Stream> GetContentWithChannelVersion(Stream content, string channelCode)
        {          
            JObject document = GetDocument(content, channelCode);

            var funding = (JObject)document["funding"];
            var fundingVersion = (string)funding["fundingVersion"];
            var channelVersion = funding["channelVersion"];

            if (channelVersion == null || channelVersion.Count() == 0)
            {
               
                var channelVersions = await GetchannelVersions(channelCode, fundingVersion);

                if (funding.ContainsKey("channelVersion"))
                {
                    funding["channelVersion"] = JToken.FromObject(channelVersions);
                }
                else
                {
                    funding.Property("fundingVersion").AddAfterSelf(new JProperty("channelVersion", JToken.FromObject(channelVersions)));
                }
                return new MemoryStream(Encoding.UTF8.GetBytes(document.ToString(Formatting.None)));
            }
            return content;
        }

        private JObject GetDocument(Stream content, string channelCode)
        {
            var reader = new StreamReader(content);
            string feedDocument = reader.ReadToEnd();
            content.Position = 0;
            reader.DiscardBufferedData();

            var providerDocument = new JsonTextReader(new StringReader(feedDocument));
            providerDocument.FloatParseHandling = FloatParseHandling.Decimal;
            JObject document = JObject.Load(providerDocument);
            return document;
        }

        public async Task<Stream> GetContentWithChannelProviderVersion(Stream content, string channelCode)
        {         
            JObject document = GetDocument(content, channelCode);

            var fundingVersion = (string)document["fundingVersion"];
            var channelVersion = document["channelVersion"];

            if (channelVersion == null || channelVersion.Count() == 0)
            {               
                var channelVersions = await GetchannelVersions(channelCode, fundingVersion);
                if (document.ContainsKey("channelVersion"))
                {
                    document["channelVersion"] = JToken.FromObject(channelVersions);
                }
                else
                {
                    document.Property("fundingVersion").AddAfterSelf(new JProperty("channelVersion", JToken.FromObject(channelVersions)));
                }
                return new MemoryStream(Encoding.UTF8.GetBytes(document.ToString(Formatting.None)));
            }
            return content;
        }

        private async Task<List<ChannelVersion>> GetchannelVersions(string channelCode, string fundingVersion)
        {
            List<ChannelVersion> channelVersions = new List<ChannelVersion>();
            var channels = await _repo.GetChannels();
            channels = channels.Where(p => p.ChannelId < (int)ChannelType.SpecToSpec);
            channels.ForEach(channel =>
            {
                channelVersions.Add(new ChannelVersion
                {
                    type = channel.ChannelName,
                    value = channel.ChannelCode.ToLower() == channelCode.ToLower() ? Convert.ToInt32(fundingVersion.Split('_')[0]) : 0
                });
            });
            return channelVersions;
        }
    }
}
