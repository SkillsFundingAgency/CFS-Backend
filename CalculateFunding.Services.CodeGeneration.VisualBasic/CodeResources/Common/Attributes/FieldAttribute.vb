Imports System
<AttributeUsage(AttributeTargets.Property)> Class FieldAttribute
    Inherits System.Attribute

    Public Property Id() As String
    Public Property Name() As String
End Class