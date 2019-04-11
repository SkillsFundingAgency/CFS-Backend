using System;
using Serilog;

namespace CalculateFunding.Services.Core.Helpers
{
    /// <summary>
    /// Method Guard Helpers
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Checks argument value to ensure it isn't null
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="parameterName">Parameter Name</param>
        public static void ArgumentNotNull(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Checks argument value to ensure it isn't null
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="message">Exception Message</param>
        public static void ArgumentNotNull(object value, string parameterName, string message)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName, message);
            }
        }

        /// <summary>
        /// Checks parameter value to ensure content is not null or empty string
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="parameterName">Parameter Name</param>
        public static void IsNullOrWhiteSpace(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Checks parameter value to ensure content is not null or empty string
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="parameterName">Parameter Name</param>
        /// <param name="message">Exception message</param>
        public static void IsNullOrWhiteSpace(string value, string parameterName, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(parameterName, message);
            }
        }

        public static void IsNullOrWhiteSpace(string value, string parameterName, string message, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                logger.Error(message);
                throw new ArgumentNullException(parameterName, message);
            }
        }
    }
}
