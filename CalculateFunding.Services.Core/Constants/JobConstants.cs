namespace CalculateFunding.Services.Core.Constants
{
    public static class JobConstants
    {
        public static class DefinitionNames
        {
            public const string CreateAllocationJob = "CreateAllocationJob";

            public const string CreateInstructAllocationJob = "CreateInstructAllocationJob";
        }

        public static class NotificationChannels
        {
            public const string All = "notifications";

            public const string SpecificationPrefix = "spec-";

            public const string ParentJobs = "parentjobs";

        }

        public const string NotificationsHubName = "notifications";

        public const string NotificationsTargetFunction = "NotificationEvent";
    }
}
