Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Repositories
Imports TheatreTicketingSystem.Utils

Namespace Services

    ''' <summary>
    ''' Logic nghiệp vụ cho việc gán ghế.
    ''' Để SeatConflictException nổi lên để UI có thể hiển thị thông báo thân thiện cho các trường hợp race condition.
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
        ''' Lưu các bản gán ghế cho một booking.
        ''' Quy tắc nghiệp vụ:
        '''   1. Số lượng ghế phải khớp chính xác với ticket_count của booking.
        '''   2. Không ghế nào được phép đã bị đặt bởi một booking khác (cùng suất diễn).
        '''
        ''' Xử lý xung đột:
        '''   - Kiểm tra xung đột ở lớp Application (Quy tắc 2) bắt các trường hợp thông thường.
        '''   - SeatConflictException từ Repository (SQLSTATE 23505) bắt trường hợp race-condition
        '''     hiếm gặp khi hai người dùng chọn cùng một ghế đồng thời.
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

            ' SeatConflictException (DB UNIQUE violation) lan truyền đến đây
            ' và được bắt riêng biệt trong frmSeatAssignment.SaveAssignments().
            _assignRepo.SaveAssignments(bookingId, booking.PerformanceId, seats)
        End Sub

    End Class

End Namespace
