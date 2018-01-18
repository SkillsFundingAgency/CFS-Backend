Imports System

Public Class RecoupmentDataset

    Public Shared DatasetDefinitionName As String = "Recoupment"

    Public Property Id As String

    Public Property BudgetId As String

    Public Property ProviderUrn As String

    Public Property ProviderName As String

    Public Property DatasetName As String

    Public Property Anomaliespositive As Decimal

    Public Property Anomaliesnegative As Decimal

    Public Property Anomaliesareapproved As Decimal

    Public Property Positive_anomalies_comment As Decimal

    Public Property Negative_anomalies_comment As Decimal
End Class
