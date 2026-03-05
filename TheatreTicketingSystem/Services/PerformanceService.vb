Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Repositories

Namespace Services

    ''' <summary>
    ''' Đóng gói tất cả các quy tắc nghiệp vụ liên quan đến Suất diễn.
    ''' total_seats được giới hạn tối đa 100 (khớp với sơ đồ 10×10 cố định).
    ''' Kiểm tra thời gian bắt đầu trong tương lai chỉ áp dụng khi Tạo mới, không áp dụng khi Cập nhật,
    ''' để các suất diễn cũ vẫn có thể chỉnh sửa các trường không liên quan đến thời gian.
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
        ''' Trả về một suất diễn theo ID.
        ''' Được sử dụng bởi frmPerformanceMaster.SelectionChanged để loại bỏ truy vấn N+1.
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
        ''' Tạo suất diễn mới.
        ''' Việc kiểm tra thời gian bắt đầu trong tương lai CHỈ áp dụng khi Tạo mới.
        ''' </summary>
        Public Function Create(p As Performance) As Performance
            ValidateCommonFields(p)
            ValidateFutureStartTime(p)   ' Quy tắc chỉ áp dụng khi Tạo mới
            p.Id = _repo.Create(p)
            Return p
        End Function

        ''' <summary>
        ''' Cập nhật suất diễn hiện có.
        ''' KHÔNG kiểm tra thời gian bắt đầu trong tương lai khi Cập nhật,
        ''' cho phép chỉnh sửa địa điểm/mô tả cho các suất diễn trong quá khứ.
        ''' </summary>
        Public Sub Update(p As Performance)
            If p.Id <= 0 Then Throw New ArgumentException("ID suất diễn không hợp lệ.")
            ValidateCommonFields(p)      ' Không kiểm tra thời gian khi chỉnh sửa
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
        ''' Xác thực các trường áp dụng cho cả Tạo mới và Cập nhật.
        ''' total_seats giới hạn ở mức 100 (sơ đồ cố định 10×10).
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
            ' Tối đa 100 ghế để phù hợp với sơ đồ 10×10 cố định
            If p.TotalSeats <= 0 OrElse p.TotalSeats > 100 Then
                Throw New ArgumentException(
                    "Tổng số ghế phải trong khoảng 1–100 (sơ đồ 10×10 cố định).")
            End If
        End Sub

        ''' <summary>
        ''' Xác thực thời gian bắt đầu phải ở tương lai.
        ''' CHỈ được gọi từ Create, không gọi từ Update.
        ''' </summary>
        Private Shared Sub ValidateFutureStartTime(p As Performance)
            If p.StartTime < DateTime.Now.AddMinutes(-5) Then
                Throw New ArgumentException(
                    "Thời gian bắt đầu phải là thời điểm trong tương lai.")
            End If
        End Sub

    End Class

End Namespace
