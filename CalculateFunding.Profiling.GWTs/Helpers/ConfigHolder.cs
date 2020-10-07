namespace CalculateFunding.Profiling.GWTs.Helpers
{
	using Microsoft.Extensions.Configuration;
	using Options;

	public static class ConfigHolder
	{
		private static WebInfo _webInfo;
		private static AzureAd _azureAd;
		private static IConfigurationRoot _configRoot;

		public static WebInfo GetWebConfigDto()
		{
			InitializeConfigRootIfNull();

			if (_webInfo == null)
			{
				WebInfo webInfo = new WebInfo();
				_configRoot
					.GetSection("WebInfo")
					.Bind(webInfo);

				_webInfo = webInfo;
			}

			return _webInfo;
		}

		public static AzureAd GetAzureAdDto()
		{
			InitializeConfigRootIfNull();

			if (_azureAd == null)
			{
				AzureAd azureAdInfo = new AzureAd();
				_configRoot
					.GetSection("AzureAD")
					.Bind(azureAdInfo);

				_azureAd = azureAdInfo;
			}

			return _azureAd;
		}

		private static void InitializeConfigRootIfNull()
		{
			if (_configRoot == null)
			{
				_configRoot = new ConfigurationBuilder()
					.AddEnvironmentVariables()
					.Build();
			}
		}

		
	}
}
