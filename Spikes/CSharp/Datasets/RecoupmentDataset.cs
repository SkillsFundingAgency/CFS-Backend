using System;

public class RecoupmentDataset
{
    public static string DatasetDefinitionName = "Recoupment";
    public string Id
    {
        get;
        set;
    }

    public string BudgetId
    {
        get;
        set;
    }

    public string ProviderUrn
    {
        get;
        set;
    }

    public string ProviderName
    {
        get;
        set;
    }

    public string DatasetName
    {
        get;
        set;
    }

    public string Anomaliespositive
    {
        get;
        set;
    }

    public string Anomaliesnegative
    {
        get;
        set;
    }

    public string Anomaliesareapproved
    {
        get;
        set;
    }

    public string Positive_anomalies_comment
    {
        get;
        set;
    }

    public string Negative_anomalies_comment
    {
        get;
        set;
    }
}