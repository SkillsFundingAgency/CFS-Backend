Public Class Datasets

    <DatasetRelationship(Id:="1", Name:="This Year")>
    Public Property ThisYear() As APTDataset

    <DatasetRelationship(Id:="2", Name:="Last Year")>
    Public Property LastYear() As APTDataset

    <DatasetRelationship(Id:="3", Name:="All Authority Providers")>
    Public Property AllAuthorityProviders() As System.Collections.Generic.List(Of APTDataset)
End Class