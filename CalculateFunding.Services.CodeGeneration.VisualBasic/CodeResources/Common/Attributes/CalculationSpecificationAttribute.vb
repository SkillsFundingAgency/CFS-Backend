Imports System
<AttributeUsage(AttributeTargets.Method Or AttributeTargets.Field)> Class CalculationSpecificationAttribute
    Inherits  System.Attribute

    Public Property Id() As String
    Public Property Name() As String

 
End Class