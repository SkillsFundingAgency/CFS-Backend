Imports System
<AttributeUsage(AttributeTargets.Property & AttributeTargets.Class & AttributeTargets.Method)> Class DescriptionAttribute
    Inherits System.Attribute

    Public Property Description() As String

End Class