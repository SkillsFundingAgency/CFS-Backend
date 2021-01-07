Imports System
Imports System.Reflection
<AttributeUsage(AttributeTargets.Property Or AttributeTargets.Class Or AttributeTargets.Method Or AttributeTargets.Field)> Class DescriptionAttribute
    Inherits System.Attribute

    Public Property Description() As String

    Public Shared Function GetEnumDescription(ByVal EnumConstant As Object) As String
        Dim field As FieldInfo = EnumConstant.GetType().GetField(EnumConstant.ToString())
        Dim attribute() As DescriptionAttribute =
                      DirectCast(field.GetCustomAttributes(GetType(DescriptionAttribute),
                      False), DescriptionAttribute())

        If attribute.Length > 0 Then
            Return attribute(0).Description
        Else
            Return EnumConstant.ToString()
        End If
    End Function
End Class