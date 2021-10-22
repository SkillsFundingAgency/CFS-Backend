namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Options
{
    public class ReleaseDefinitionOptions
    {
        public string[] ReleaseDefinitionNames { get; set; }

        public ReleaseDefinitionStageOptions[] ReleaseDefinitionStages { get; set; }
    }
}
