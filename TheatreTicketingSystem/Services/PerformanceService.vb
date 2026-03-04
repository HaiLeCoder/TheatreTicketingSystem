Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Repositories

Namespace Services

    ''' <summary>
    ''' Encapsulates all business rules related to Performances.
    ''' RC-010 FIX: total_seats capped at 100 (matching the fixed 10×10 seat grid).
    ''' RC-012 FIX: StartTime future-check only applied on Create, not on Update,
    '''             so past performances can still be edited for non-time fields.
    ''' </summary>
    Public Class PerformanceService

        Private Shared _instance As PerformanceService
        Private Shared ReadOnly _lock As New Object()

        Private ReadOnly _repo As PerformanceRepository

        Private Sub New()
            _repo = PerformanceRepository.Instance
        End Sub

        Public Shared ReadOnly Property Instance As PerformanceService
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New PerformanceService()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ' ── Queries ───────────────────────────────────────────────────────────

        ''' <summary>Returns all active performances.</summary>
        Public Function GetAll() As List(Of Performance)
            Return _repo.GetAll()
        End Function

        ''' <summary>
        ''' Returns a single performance by ID.
        ''' RC-006 FIX: Used by frmPerformanceMaster.SelectionChanged
        '''             instead of GetAll().FirstOrDefault() (eliminates N+1 query).
        ''' </summary>
        Public Function GetById(id As Integer) As Performance
            Return _repo.GetById(id)
        End Function

        ''' <summary>Searches performances by name and/or date range.</summary>
        Public Function Search(nameFilter As String,
                               fromDate As DateTime?,
                               toDate As DateTime?) As List(Of Performance)
            If fromDate.HasValue AndAlso toDate.HasValue AndAlso fromDate.Value > toDate.Value Then
                Throw New ArgumentException("Ngày bắt đầu không được lớn hơn ngày kết thúc.")
            End If
            Return _repo.Search(nameFilter, fromDate, toDate)
        End Function

        ' ── Commands ──────────────────────────────────────────────────────────

        ''' <summary>
        ''' Creates a new performance.
        ''' RC-012 FIX: StartTime future-check is ONLY applied on Create.
        ''' </summary>
        Public Function Create(p As Performance) As Performance
            ValidateCommonFields(p)
            ValidateFutureStartTime(p)   ' RC-012: Create-only rule
            p.Id = _repo.Create(p)
            Return p
        End Function

        ''' <summary>
        ''' Updates an existing performance.
        ''' RC-012 FIX: StartTime future-check NOT applied on Update,
        '''             allowing editing of location/description for past performances.
        ''' </summary>
        Public Sub Update(p As Performance)
            If p.Id <= 0 Then Throw New ArgumentException("ID suất diễn không hợp lệ.")
            ValidateCommonFields(p)      ' RC-012: No time check on edit
            _repo.Update(p)
        End Sub

        ''' <summary>
        ''' Soft-deletes a performance. Raises if it has confirmed bookings.
        ''' </summary>
        Public Sub Delete(id As Integer)
            If _repo.HasBookings(id) Then
                Throw New InvalidOperationException(
                    "Không thể xóa suất diễn đã có booking xác nhận." &
                    Environment.NewLine &
                    "Vui lòng huỷ tất cả booking trước khi xóa.")
            End If
            _repo.Delete(id)
        End Sub

        ' ── Private validation helpers ────────────────────────────────────────

        ''' <summary>
        ''' Validates fields that apply BOTH to Create and Update.
        ''' RC-010 FIX: total_seats capped at 100 (10×10 fixed grid).
        ''' </summary>
        Private Shared Sub ValidateCommonFields(p As Performance)
            If String.IsNullOrWhiteSpace(p.Name) Then
                Throw New ArgumentException("Tên vở diễn không được để trống.")
            End If
            If p.Name.Length > 200 Then
                Throw New ArgumentException("Tên vở diễn không vượt quá 200 ký tự.")
            End If
            If p.DurationMinutes <= 0 Then
                Throw New ArgumentException("Thời lượng phải lớn hơn 0 phút.")
            End If
            ' RC-010 FIX: Max 100 to match fixed 10×10 seat grid
            If p.TotalSeats <= 0 OrElse p.TotalSeats > 100 Then
                Throw New ArgumentException(
                    "Tổng số ghế phải trong khoảng 1–100 (sơ đồ 10×10 cố định).")
            End If
        End Sub

        ''' <summary>
        ''' Validates that start time is in the future.
        ''' RC-012 FIX: Called ONLY from Create, not from Update.
        ''' </summary>
        Private Shared Sub ValidateFutureStartTime(p As Performance)
            If p.StartTime < DateTime.Now.AddMinutes(-5) Then
                Throw New ArgumentException(
                    "Thời gian bắt đầu phải là thời điểm trong tương lai.")
            End If
        End Sub

    End Class

End Namespace
