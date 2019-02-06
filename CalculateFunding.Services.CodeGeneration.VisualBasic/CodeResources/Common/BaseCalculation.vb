Imports System.Collections.Generic

Public Class BaseCalculation

    Public Property Aggregations As Dictionary(Of String, Decimal)

    Public Property CalcResultsCache As Dictionary(Of String, System.Nullable(Of Decimal))

#Region "Legacy Store Support"

    Public Property rid As String
    Public Property currentscenario As Scenario

    Public Sub Print(Of T)(value As T, name As String, rid As String)

    End Sub

    Public Function LAToProv(Of T)(value As T) As T
        Return value
    End Function

    Public Function IIf(Of T)(value As T, one As Boolean, two As Boolean) As T
        Return value
    End Function

#End Region

    Public Function Exclude() As System.Nullable(Of Decimal)
        Return Nothing
    End Function

    Public Function Sum(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName + "_Sum") Then
            Return Aggregations.Item(fieldName + "_Sum")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function

    Public Function Avg(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName + "_Average") Then
            Return Aggregations.Item(fieldName + "_Average")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function

    Public Function Min(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName + "_Min") Then
            Return Aggregations.Item(fieldName + "_Min")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function

    Public Function Max(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName + "_Max") Then
            Return Aggregations.Item(fieldName + "_Max")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function
End Class