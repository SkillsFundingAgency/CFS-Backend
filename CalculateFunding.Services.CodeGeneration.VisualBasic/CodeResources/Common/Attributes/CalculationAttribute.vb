Imports System
<AttributeUsage(AttributeTargets.Method Or AttributeTargets.Field)> Class CalculationAttribute
    Inherits System.Attribute

    Public Property Id() As String
    Public Property Name() As String
    Public Property CalculationType() As String
    Public Property CalculationDataType() As String
End Class