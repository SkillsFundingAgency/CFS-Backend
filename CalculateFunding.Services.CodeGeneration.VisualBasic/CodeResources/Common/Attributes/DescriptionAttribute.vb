Imports System
<AttributeUsage(AttributeTargets.Property Or AttributeTargets.Class Or AttributeTargets.Method Or AttributeTargets.Field)> Class DescriptionAttribute
    Inherits System.Attribute

    Public Property Description() As String

End Class