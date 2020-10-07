// namespace CalculateFunding.Api.Profiling.Extensions
// {
// 	using System;
// 	using Logging;
// 	using Microsoft.ApplicationInsights;
// 	using Microsoft.ApplicationInsights.Extensibility;
// 	using Microsoft.Extensions.Configuration;
// 	using Microsoft.Extensions.DependencyInjection;
// 	using Options;
// 	using Serilog;
// 	using Serilog.Core;
// 	using Serilog.Events;
//
// 	public static class ServiceCollectionExtensions
// 	{
// 		public static IServiceCollection AddLogging(this IServiceCollection builder, string serviceName, IConfigurationRoot config = null)
// 		{
// 			builder.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
//
// 			builder.AddSingleton<Serilog.ILogger>((ctx) =>
// 			{
// 				TelemetryClient client = ctx.GetService<TelemetryClient>();
//
// 				LoggerConfiguration loggerConfiguration = GetLoggerConfiguration(client, serviceName);
//
// 				if (config != null && !string.IsNullOrWhiteSpace(config.GetValue<string>("FileLoggingPath")))
// 				{
// 					string folderPath = config.GetValue<string>("FileLoggingPath");
//
// 					loggerConfiguration.WriteTo.RollingFile(folderPath + "log-{Date}-" + Environment.MachineName + ".txt", LogEventLevel.Verbose);
// 				}
//
// 				return loggerConfiguration.CreateLogger();
// 			});
//
// 			return builder;
// 		}
//
// 		public static IServiceCollection AddApplicationInsightsTelemetryClient(this IServiceCollection builder, IConfiguration config, string serviceName)
// 		{
// 			ApplicationInsightsOptions appInsightsOptions = new ApplicationInsightsOptions();
//
// 			config.Bind("ApplicationInsightsOptions", appInsightsOptions);
//
//
// 			string appInsightsKey = appInsightsOptions.InstrumentationKey;
//
// 			if (string.IsNullOrWhiteSpace(appInsightsKey))
// 			{
// 				throw new InvalidOperationException("Unable to lookup Application Insights Configuration key from Configuration Provider. The value returned was empty string");
// 			}
//
// 			TelemetryClient telemetryClient = new TelemetryClient(new TelemetryConfiguration
// 			{
// 				InstrumentationKey = appInsightsKey,
// 			}) {InstrumentationKey = appInsightsKey};
//
// 			if (!telemetryClient.Context.GlobalProperties.ContainsKey(LoggingConstants.ServiceNamePropertiesName))
// 			{
// 				telemetryClient.Context.GlobalProperties.Add(LoggingConstants.ServiceNamePropertiesName, serviceName);
// 			}
//
// 			builder.AddSingleton(telemetryClient);
//
// 			return builder;
// 		}
//
// 		public static LoggerConfiguration GetLoggerConfiguration(TelemetryClient telemetryClient, string serviceName)
// 		{
// 			return new LoggerConfiguration()
// 				.Enrich.With(new ServiceNameLogEnricher(serviceName))
// 				.WriteTo.ApplicationInsightsTraces(telemetryClient, LogEventLevel.Verbose, null, null);
// 		}
// 	}
// }
