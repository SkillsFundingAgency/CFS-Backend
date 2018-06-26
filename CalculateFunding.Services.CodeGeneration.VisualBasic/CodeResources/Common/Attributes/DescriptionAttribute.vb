Imports System
<AttributeUsage(AttributeTargets.Property Or AttributeTargets.Class Or AttributeTargets.Method)> Class DescriptionAttribute
    Inherits System.Attribute

    Public Property Description() As String

End Class