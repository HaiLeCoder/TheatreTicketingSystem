Namespace Models

    ''' <summary>
    ''' Represents a specific seat assigned to a booking.
    ''' Maps to the "seat_assignments" table.
    ''' </summary>
    Public Class SeatAssignment
        Public Property Id As Integer
        Public Property BookingId As Integer
        Public Property PerformanceId As Integer
        Public Property RowLabel As Char      ' A–J
        Public Property ColNumber As Integer  ' 1–10
        Public Property AssignedAt As DateTime

        ''' <summary>Returns a combined seat label, e.g. "A5".</summary>
        Public ReadOnly Property SeatLabel As String
            Get
                Return $"{RowLabel}{ColNumber}"
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return SeatLabel
        End Function
    End Class

End Namespace
