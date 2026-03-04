Namespace Models

    ''' <summary>
    ''' Lookup entity for seat categories (e.g. Thường, VIP, Đôi).
    ''' Maps to the "seat_types" table.
    ''' </summary>
    Public Class SeatType
        Public Property Id As Integer
        Public Property Name As String = String.Empty
        Public Property Price As Decimal
        Public Property Description As String = String.Empty
        Public Property CreatedAt As DateTime

        Public Overrides Function ToString() As String
            Return $"{Name} - {Price:N0} VNĐ"
        End Function
    End Class

End Namespace
