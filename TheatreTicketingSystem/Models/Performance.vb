Imports System.ComponentModel.DataAnnotations

Namespace Models

    ''' <summary>
    ''' Represents a theatre performance/show.
    ''' Maps to the "performances" table.
    ''' </summary>
    Public Class Performance
        Public Property Id As Integer
        Public Property Name As String = String.Empty
        Public Property StartTime As DateTime
        Public Property DurationMinutes As Integer
        Public Property Location As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property TotalSeats As Integer
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime
        Public Property UpdatedAt As DateTime

        ''' <summary>Returns the end time, derived from start + duration.</summary>
        Public ReadOnly Property EndTime As DateTime
            Get
                Return StartTime.AddMinutes(DurationMinutes)
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"{Name} ({StartTime:dd/MM/yyyy HH:mm})"
        End Function
    End Class

End Namespace
