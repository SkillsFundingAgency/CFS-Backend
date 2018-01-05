Imports System

<AttributeUsage(AttributeTargets.Method)> Class CalculationAttribute
    Inherits Attribute

    Public Property CalculationId() As String
    Public Property CalculationName() As String
 
    Public Property PolicyId() As String
    Public Property PolicyName() As String

    Public Property AllocationLineId() As String
    Public Property AllocationLineName() As String
 
    Sub New()
    End Sub
 
End Class