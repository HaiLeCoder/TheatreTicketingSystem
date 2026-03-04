Namespace Models

    ''' <summary>
    ''' Booking status enumeration.
    ''' </summary>
    Public Enum BookingStatus
        PENDING
        CONFIRMED
        CANCELLED
    End Enum

    ''' <summary>
    ''' Represents a customer booking.
    ''' Maps to the "bookings" table.
    ''' </summary>
    Public Class Booking
        Public Property Id As Integer
        Public Property PerformanceId As Integer
        Public Property SeatTypeId As Integer
        Public Property CustomerName As String = String.Empty
        Public Property CustomerPhone As String = String.Empty
        Public Property TicketCount As Integer
        Public Property TotalAmount As Decimal
        Public Property Status As BookingStatus = BookingStatus.PENDING
        Public Property Notes As String = String.Empty
        Public Property CreatedAt As DateTime
        Public Property UpdatedAt As DateTime

        ' Navigation / join properties (populated by service/repository)
        Public Property PerformanceName As String = String.Empty
        Public Property SeatTypeName As String = String.Empty
        Public Property SeatsAssigned As Integer

        Public ReadOnly Property StatusDisplay As String
            Get
                Select Case Status
                    Case BookingStatus.PENDING    : Return "Chờ xác nhận"
                    Case BookingStatus.CONFIRMED  : Return "Đã xác nhận"
                    Case BookingStatus.CANCELLED  : Return "Đã huỷ"
                    Case Else                      : Return Status.ToString()
                End Select
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"#{Id} - {CustomerName} ({TicketCount} vé)"
        End Function
    End Class

End Namespace
