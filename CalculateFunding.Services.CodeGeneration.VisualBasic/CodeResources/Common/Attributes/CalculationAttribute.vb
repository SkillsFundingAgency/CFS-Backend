Imports System
<AttributeUsage(AttributeTargets.Method)> Class CalculationAttribute
    Inherits  System.Attribute

    Public Property Id() As String
    Public Property Name() As String

End Class