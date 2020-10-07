﻿namespace CalculateFunding.Profiling.ConsoleConfig.Persistence
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using Microsoft.Azure.Documents.Client;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;

	public static class CosmosHelper
	{
		private static readonly ConnectionPolicy ConnectionPolicy = new ConnectionPolicy
		{
			ConnectionMode = ConnectionMode.Direct,
			ConnectionProtocol = Protocol.Tcp,
			RequestTimeout = new TimeSpan(1, 0, 0),
			MaxConnectionLimit = 300,
			//RetryOptions = new RetryOptions
			//{
			//    MaxRetryAttemptsOnThrottledRequests = 10,
			//    MaxRetryWaitTimeInSeconds = 60
			//}
		};

		public static DocumentClient Parse(string connectionString)
		{
			if (String.IsNullOrWhiteSpace(connectionString))
			{
				throw new ArgumentException("Connection string cannot be empty.");
			}

			connectionString = connectionString.Trim();

			if (ParseImpl(connectionString, out var ret, err => throw new FormatException(err)))
			{
				return ret;
			}

			throw new ArgumentException($"Connection string was not able to be parsed into a document client.");
		}

		private const string AccountEndpointKey = "AccountEndpoint";
		private const string AccountKeyKey = "AccountKey";
		private static readonly HashSet<string> RequireSettings = new HashSet<string>(new[] { AccountEndpointKey, AccountKeyKey }, StringComparer.OrdinalIgnoreCase);

		internal static bool ParseImpl(string connectionString, out DocumentClient documentClient, Action<string> error)
		{
			IDictionary<string, string> settings = ParseStringIntoSettings(connectionString, error);

			if (settings == null)
			{
				documentClient = null;
				return false;
			}

			if (!RequireSettings.IsSubsetOf(settings.Keys))
			{
				documentClient = null;
				return false;
			}

			var jsonSettings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};

			documentClient = new DocumentClient(new Uri(settings[AccountEndpointKey]), settings[AccountKeyKey], ConnectionPolicy);
			return true;
		}

		/// <summary>
		/// Tokenizes input and stores name value pairs.
		/// </summary>
		/// <param name="connectionString">The string to parse.</param>
		/// <param name="error">Error reporting delegate.</param>
		/// <returns>Tokenized collection.</returns>
		private static IDictionary<string, string> ParseStringIntoSettings(string connectionString, Action<string> error)
		{
			IDictionary<string, string> settings = new Dictionary<string, string>();
			string[] splitted = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var nameValue in splitted)
			{
				var splittedNameValue = nameValue.Split(new[] { '=' }, 2);

				if (splittedNameValue.Length != 2)
				{
					error("Settings must be of the form \"name=value\".");
					return null;
				}

				if (settings.ContainsKey(splittedNameValue[0]))
				{
					error(string.Format(CultureInfo.InvariantCulture, "Duplicate setting '{0}' found.", splittedNameValue[0]));
					return null;
				}

				settings.Add(splittedNameValue[0], splittedNameValue[1]);
			}

			return settings;
		}
	}
}
