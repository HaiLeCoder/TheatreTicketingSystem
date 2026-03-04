Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Repositories
Imports TheatreTicketingSystem.Utils

Namespace Services

    ''' <summary>
    ''' Business logic for seat assignment.
    ''' RC-005 FIX: SaveAssignments now lets SeatConflictException bubble up
    '''             so the UI can display a friendly message for race conditions.
    ''' </summary>
    Public Class SeatAssignmentService

        Private Shared _instance As SeatAssignmentService
        Private Shared ReadOnly _lock As New Object()

        Private ReadOnly _assignRepo  As SeatAssignmentRepository
        Private ReadOnly _bookingRepo As BookingRepository

        Private Sub New()
            _assignRepo  = SeatAssignmentRepository.Instance
            _bookingRepo = BookingRepository.Instance
        End Sub

        Public Shared ReadOnly Property Instance As SeatAssignmentService
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New SeatAssignmentService()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ' ── Queries ───────────────────────────────────────────────────────────

        ''' <summary>
        ''' Returns a seat-state map for the entire 10×10 grid of a performance.
        ''' Key = "RowCol" (e.g. "A5"), Value = bookingId that owns it (0 = free).
        ''' </summary>
        Public Function GetSeatMap(performanceId As Integer,
                                   currentBookingId As Integer) As Dictionary(Of String, Integer)
            Dim map As New Dictionary(Of String, Integer)

            ' Initialise all 100 seats as free
            For r = 0 To 9
                Dim rowChar = Chr(Asc("A"c) + r)
                For c = 1 To 10
                    map($"{rowChar}{c}") = 0
                Next
            Next

            ' Mark occupied seats with their booking ID
            Dim occupied = _assignRepo.GetByPerformance(performanceId)
            For Each sa In occupied
                Dim key = $"{sa.RowLabel}{sa.ColNumber}"
                map(key) = sa.BookingId
            Next

            Return map
        End Function

        ''' <summary>Returns seats already assigned to a booking.</summary>
        Public Function GetAssignedSeats(bookingId As Integer) As List(Of SeatAssignment)
            Return _assignRepo.GetByBooking(bookingId)
        End Function

        ' ── Commands ──────────────────────────────────────────────────────────

        ''' <summary>
        ''' Saves seat assignments for a booking.
        ''' Business rules:
        '''   1. Seat count must exactly equal booking's ticket_count.
        '''   2. No seat may already be taken by another booking (same performance).
        '''
        ''' RC-005 FIX:
        '''   - Application-layer collision check (Rule 2) catches the common case.
        '''   - SeatConflictException from Repository (SQLSTATE 23505) catches the
        '''     rare race-condition case where two users select the same seat concurrently.
        '''   Both result in a user-friendly InvalidOperationException-like message.
        ''' </summary>
        Public Sub SaveAssignments(bookingId As Integer,
                                   seats As List(Of SeatAssignment))

            ' Load booking to get ticket count and performance ID
            Dim booking = _bookingRepo.GetById(bookingId)
            If booking Is Nothing Then
                Throw New KeyNotFoundException($"Booking #{bookingId} không tồn tại.")
            End If

            ' Rule 1: Exact seat count
            If seats.Count <> booking.TicketCount Then
                Throw New InvalidOperationException(
                    $"Booking yêu cầu đúng {booking.TicketCount} ghế, " &
                    $"nhưng bạn đã chọn {seats.Count} ghế.")
            End If

            ' Rule 2: Application-level collision check (fast path)
            Dim occupied = _assignRepo.GetOccupiedSeats(booking.PerformanceId, bookingId)
            Dim conflicts As New List(Of String)
            For Each sa In seats
                Dim key = $"{sa.RowLabel}{sa.ColNumber}"
                If occupied.Contains(key) Then conflicts.Add(key)
            Next
            If conflicts.Count > 0 Then
                Throw New InvalidOperationException(
                    $"Các ghế đã được booking khác đặt: {String.Join(", ", conflicts)}." &
                    Environment.NewLine & "Vui lòng chọn lại ghế.")
            End If

            ' Stamp booking/performance IDs on each seat
            For Each sa In seats
                sa.PerformanceId = booking.PerformanceId
                sa.BookingId     = bookingId
            Next

            ' RC-005: SeatConflictException (DB UNIQUE violation) propagates here
            '         and is caught separately in frmSeatAssignment.SaveAssignments().
            _assignRepo.SaveAssignments(bookingId, booking.PerformanceId, seats)
        End Sub

    End Class

End Namespace
