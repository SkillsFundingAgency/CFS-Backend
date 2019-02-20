Namespace Microsoft.VisualBasic.CompilerServices

    Public Class ProjectData

        Public Shared Sub SetProjectError(ex As Exception)

        End Sub

        Public Shared Sub SetProjectError(ex As Exception, lErl As Int32)

        End Sub

        Public Shared Sub ClearProjectError()

        End Sub

        Public Shared Function CreateProjectError(hr As Int32) As Exception
            Return New Exception
        End Function

    End Class

End Namespace
