Imports System.Collections.Generic

Public Class BaseCalculation
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Property Aggregations As Dictionary(Of String, Decimal)

    Public Property rid As String

    Public Function Exclude() As System.Nullable(Of Decimal)
        Return Nothing
    End Function

    Public Function Sum(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName) Then
            Return Aggregations.Item(fieldName)
        ElseIf Aggregations.ContainsKey(fieldName + "_Sum") Then
            Return Aggregations.Item(fieldName + "_Sum")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function

    Public Function Avg(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName) Then
            Return Aggregations.Item(fieldName)
        ElseIf Aggregations.ContainsKey(fieldName + "_Average") Then
            Return Aggregations.Item(fieldName + "_Average")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function

    Public Function Min(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName) Then
            Return Aggregations.Item(fieldName)
        ElseIf Aggregations.ContainsKey(fieldName + "_Min") Then
            Return Aggregations.Item(fieldName + "_Min")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function

    Public Function Max(ByVal fieldName As String) As Decimal
        If Aggregations.ContainsKey(fieldName) Then
            Return Aggregations.Item(fieldName)
        ElseIf Aggregations.ContainsKey(fieldName + "_Max") Then
            Return Aggregations.Item(fieldName + "_Max")
        End If
        Throw New System.ArgumentException(fieldName + " does not have an aggregated value")
    End Function
End Class