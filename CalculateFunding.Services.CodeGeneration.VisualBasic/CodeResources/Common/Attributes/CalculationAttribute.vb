Imports System
<AttributeUsage(AttributeTargets.Method)> Class CalculationAttribute
    Inherits  System.Attribute

    Public Property Id() As String
    Public Property Name() As String
    Public Property CalculationType() As String
End Class