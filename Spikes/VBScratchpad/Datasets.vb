Imports System
Public Class APTProviderInformationDataset

    Public Shared DatasetDefinitionName As String = "APT Provider Information"

    Public Property Id As String
    Public Property BudgetId As String
    Public Property ProviderUrn As String
    Public Property ProviderName As String
    Public Property DatasetName As String
    Public Property UPIN As String
    Public Property DateOpened As DateTime
    Public Property LocalAuthority As String
    Public Property Phase As String
End Class

Public Class APTBasicEntitlementDataset
    Public Shared DatasetDefinitionName As String = "APT Basic Entitlement"
    Public Property Id As String
    Public Property BudgetId As String
    Public Property ProviderUrn As String
    Public Property ProviderName As String
    Public Property DatasetName As String
    Public Property PrimaryAmountPerPupil As Decimal
    Public Property PrimaryAmount As Decimal
    Public Property PrimaryNotionalSEN As Decimal
End Class

Public Class CensusNumberCountsDataset  


        Public Shared DatasetDefinitionName As String = "Census Number Counts"
Public Property Id As String
Public Property BudgetId As String
Public Property ProviderUrn As String
Public Property ProviderName As String
    Public Property DatasetName As String
    Public Property NORPrimary As Integer
End Class   