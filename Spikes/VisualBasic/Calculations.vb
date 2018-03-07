Imports System
Imports System.Collections.Generic

Public Class Calculations
    Inherits BaseCalculation

    Public Property Datasets As Datasets

    <Calculation(Id:="12344", Name:="Get Me A Dataset!")>
    Public Function GetMeADataset As Decimal
#ExternalSource("12344|Get Me A Dataset!", 1)
        Return Datasets.ThisYear.NORPrimary + Datasets.LastYear.NORPrimary
#End ExternalSource
    End Function

    <Calculation(Id:="12344", Name:="Aggregate This!")>
    Public Function AggregateThis As Decimal
#ExternalSource("12344|Aggregate This!", 1)
        Return Datasets.AllAuthorityProviders.Count
#End ExternalSource
    End Function
End Class
