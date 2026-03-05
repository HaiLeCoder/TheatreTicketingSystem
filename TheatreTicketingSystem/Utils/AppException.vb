Namespace Utils

    ''' <summary>
    ''' Tách biệt ngoại lệ Repository khỏi nội bộ Npgsql.
    ''' </summary>
    Public Class DataAccessException
        Inherits Exception

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class

    ''' <summary>
    ''' Thrown when a unique-constraint violation is detected on seat assignment.
    ''' </summary>
    Public Class SeatConflictException
        Inherits Exception

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub
    End Class

End Namespace
