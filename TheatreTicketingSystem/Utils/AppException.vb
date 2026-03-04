Namespace Utils

    ''' <summary>
    ''' Thrown when a data access operation fails.
    ''' Wraps low-level Npgsql exceptions so upper layers receive friendly messages.
    ''' RC-009: Introduced to decouple Repository exceptions from Npgsql internals.
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
    ''' RC-005: Provides a user-friendly alternative to raw NpgsqlException 23505.
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
