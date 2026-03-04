Imports System.Text.RegularExpressions
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Repositories

Namespace Services

    ''' <summary>
    ''' Business logic for creating and managing ticket bookings.
    ''' RC-015 FIX: Added phone number format validation.
    ''' </summary>
    Public Class BookingService

        Private Shared _instance As BookingService
        Private Shared ReadOnly _lock As New Object()

        Private ReadOnly _bookingRepo  As BookingRepository
        Private ReadOnly _seatTypeRepo As SeatTypeRepository

        ' RC-015: Phone regex – accepts 8–15 digits, optional leading + or spaces/dashes
        Private Shared ReadOnly PhoneRegex As New Regex(
            "^[\+]?[0-9]{8,15}$",
            RegexOptions.Compiled)

        Private Sub New()
            _bookingRepo  = BookingRepository.Instance
            _seatTypeRepo = SeatTypeRepository.Instance
        End Sub

        Public Shared ReadOnly Property Instance As BookingService
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New BookingService()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ' ── Queries ───────────────────────────────────────────────────────────

        ''' <summary>Returns all bookings.</summary>
        Public Function GetAll() As List(Of Booking)
            Return _bookingRepo.GetAll()
        End Function

        ''' <summary>Returns bookings for a specific performance.</summary>
        Public Function GetByPerformance(performanceId As Integer) As List(Of Booking)
            Return _bookingRepo.GetByPerformance(performanceId)
        End Function

        ''' <summary>Returns a booking by ID (throws KeyNotFoundException if not found).</summary>
        Public Function GetById(id As Integer) As Booking
            Dim b = _bookingRepo.GetById(id)
            If b Is Nothing Then
                Throw New KeyNotFoundException($"Booking #{id} không tồn tại.")
            End If
            Return b
        End Function

        ''' <summary>Returns all available seat types.</summary>
        Public Function GetSeatTypes() As List(Of SeatType)
            Return _seatTypeRepo.GetAll()
        End Function

        ' ── Commands ──────────────────────────────────────────────────────────

        ''' <summary>
        ''' Creates a new booking, automatically calculating total amount.
        ''' RC-015 FIX: Validates phone number format before saving.
        ''' </summary>
        Public Function Create(b As Booking) As Booking
            ValidateBooking(b)

            Dim seatType = _seatTypeRepo.GetById(b.SeatTypeId)
            If seatType Is Nothing Then
                Throw New ArgumentException("Loại ghế không hợp lệ.")
            End If

            b.TotalAmount = seatType.Price * b.TicketCount
            b.Status      = BookingStatus.PENDING
            b.Id          = _bookingRepo.Create(b)
            Return b
        End Function

        ''' <summary>Cancels a booking.</summary>
        Public Sub Cancel(id As Integer)
            Dim b = GetById(id)
            If b.Status = BookingStatus.CANCELLED Then
                Throw New InvalidOperationException("Booking đã bị huỷ trước đó.")
            End If
            _bookingRepo.UpdateStatus(id, BookingStatus.CANCELLED)
        End Sub

        ''' <summary>
        ''' Confirms a booking. Requires all seats to be assigned first.
        ''' </summary>
        Public Sub Confirm(id As Integer)
            Dim b        = GetById(id)
            Dim assigned = _bookingRepo.GetAssignedSeatCount(id)
            If assigned < b.TicketCount Then
                Throw New InvalidOperationException(
                    $"Cần gán đủ {b.TicketCount} ghế trước khi xác nhận. " &
                    $"Hiện tại đã gán {assigned} ghế.")
            End If
            _bookingRepo.UpdateStatus(id, BookingStatus.CONFIRMED)
        End Sub

        ' ── Private validation ────────────────────────────────────────────────

        Private Shared Sub ValidateBooking(b As Booking)
            If String.IsNullOrWhiteSpace(b.CustomerName) Then
                Throw New ArgumentException("Tên khách hàng không được để trống.")
            End If
            If b.CustomerName.Length > 200 Then
                Throw New ArgumentException("Tên khách hàng không vượt quá 200 ký tự.")
            End If

            ' RC-015 FIX: Validate phone format
            If Not String.IsNullOrEmpty(b.CustomerPhone) Then
                Dim cleanPhone = b.CustomerPhone.Replace(" ", "").Replace("-", "")
                If Not PhoneRegex.IsMatch(cleanPhone) Then
                    Throw New ArgumentException(
                        "Số điện thoại không hợp lệ. " &
                        "Vui lòng nhập 8–15 chữ số (có thể bắt đầu bằng +).")
                End If
            End If

            If b.PerformanceId <= 0 Then
                Throw New ArgumentException("Vui lòng chọn suất diễn.")
            End If
            If b.SeatTypeId <= 0 Then
                Throw New ArgumentException("Vui lòng chọn loại ghế.")
            End If
            If b.TicketCount <= 0 Then
                Throw New ArgumentException("Số lượng vé phải lớn hơn 0.")
            End If
            If b.TicketCount > 100 Then
                Throw New ArgumentException("Số lượng vé không vượt quá 100 vé mỗi lần đặt.")
            End If
        End Sub

    End Class

End Namespace
