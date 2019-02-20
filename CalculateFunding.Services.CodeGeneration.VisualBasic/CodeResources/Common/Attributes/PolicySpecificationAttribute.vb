Imports System

<AttributeUsage(AttributeTargets.Method Or AttributeTargets.Field, AllowMultiple:=True)> Class PolicySpecificationAttribute
    Inherits Attribute

    Public Property Id() As String
    Public Property Name() As String
 

End Class