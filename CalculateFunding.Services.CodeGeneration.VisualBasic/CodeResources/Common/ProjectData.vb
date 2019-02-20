Namespace Microsoft.VisualBasic.CompilerServices

    Public Class ProjectData

        Public Shared Sub SetProjectError(ex As System.Exception)

        End Sub

        Public Shared Sub SetProjectError(ex As System.Exception, lErl As System.Int32)

        End Sub

        Public Shared Sub ClearProjectError()

        End Sub

        Public Shared Function CreateProjectError(hr As System.Int32) As System.Exception
            Return New System.Exception
        End Function

    End Class

End Namespace
