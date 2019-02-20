Imports System
<AttributeUsage(AttributeTargets.Method Or AttributeTargets.Field)> Class AllocationLineAttribute
    Inherits  System.Attribute

    Public Property Id() As String
    Public Property Name() As String

End Class