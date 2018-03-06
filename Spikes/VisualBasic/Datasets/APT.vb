Imports System

Public Class APTDataset

    Public Shared DatasetDefinitionName As String = "APT"

    <Field(Id:="", Name:="UPIN")>
    Public Property UPIN() As String

    <Field(Id:="", Name:="Date Opened")>
    Public Property DateOpened() As DateTime

    <Field(Id:="", Name:="Phase")>
    Public Property Phase() As String

    <Field(Id:="", Name:="Acedemy Type")>
    Public Property AcedemyType() As String

    <Field(Id:="", Name:="NOR Primary")>
    Public Property NORPrimary() As Integer

    <Field(Id:="", Name:="Average Year Group Size")>
    Public Property AverageYearGroupSize() As Decimal
End Class
