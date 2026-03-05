Imports Dapper
Imports Npgsql
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Infrastructure
Imports TheatreTicketingSystem.Utils

Namespace Repositories

    ''' <summary>
    ''' Repository cho các thao tác CRUD trên bảng "bookings".
    ''' Trích xuất hằng số BASE_BOOKING_SELECT để tái sử dụng SQL.
    ''' </summary>
    Public Class BookingRepository

        Private Shared _instance As BookingRepository
        Private Shared ReadOnly _lock As New Object()

        ' Nguồn Sự Thật Duy Nhất cho projection SELECT
        Private Const BASE_BOOKING_SELECT As String =
            "SELECT b.id, b.performance_id AS PerformanceId, b.seat_type_id AS SeatTypeId," &
            "       b.customer_name AS CustomerName, b.customer_phone AS CustomerPhone," &
            "       b.ticket_count AS TicketCount, b.total_amount AS TotalAmount," &
            "       b.status, b.notes, b.created_at AS CreatedAt, b.updated_at AS UpdatedAt," &
            "       p.name AS PerformanceName, st.name AS SeatTypeName," &
            "       (SELECT COUNT(*) FROM seat_assignments sa WHERE sa.booking_id = b.id) AS SeatsAssigned " &
            "FROM bookings b " &
            "JOIN performances p  ON p.id  = b.performance_id " &
            "JOIN seat_types   st ON st.id = b.seat_type_id "

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property Instance As BookingRepository
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New BookingRepository()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ' ── Queries ───────────────────────────────────────────────────────────

        ''' <summary>Returns all bookings with joined data.</summary>
        Public Function GetAll() As List(Of Booking)
            ' Sử dụng BASE_BOOKING_SELECT dùng chung
            Dim sql = BASE_BOOKING_SELECT & "ORDER BY b.created_at DESC"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    ' Không sử dụng splitOn cho Query đơn kiểu
                    Return conn.Query(Of Booking)(sql).AsList()
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải danh sách booking: {ex.Message}", ex)
            Catch ex As Exception When Not TypeOf ex Is DataAccessException
                Throw New DataAccessException($"Lỗi không xác định khi tải booking: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>Returns bookings for a specific performance.</summary>
        Public Function GetByPerformance(performanceId As Integer) As List(Of Booking)
            Dim sql = BASE_BOOKING_SELECT &
                      "WHERE b.performance_id = @PerformanceId AND b.status <> 'CANCELLED' " &
                      "ORDER BY b.created_at DESC"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.Query(Of Booking)(sql, New With {.PerformanceId = performanceId}).AsList()
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải booking theo suất diễn: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>Returns a single booking by ID. Returns Nothing if not found.</summary>
        Public Function GetById(id As Integer) As Booking
            Dim sql = BASE_BOOKING_SELECT & "WHERE b.id = @Id"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.QuerySingleOrDefault(Of Booking)(sql, New With {.Id = id})
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải booking #{id}: {ex.Message}", ex)
            End Try
        End Function

        ' ── Commands ──────────────────────────────────────────────────────────

        ''' <summary>
        ''' Thêm booking mới và trả về ID được tạo.
        ''' </summary>
        Public Function Create(b As Booking) As Integer
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Dim cmd = conn.CreateCommand()
                    cmd.CommandText =
                        "INSERT INTO bookings (performance_id, seat_type_id, customer_name, " &
                        "  customer_phone, ticket_count, total_amount, status, notes) " &
                        "VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8) RETURNING id"

                    cmd.Parameters.AddWithValue("p1", b.PerformanceId)
                    cmd.Parameters.AddWithValue("p2", b.SeatTypeId)
                    cmd.Parameters.AddWithValue("p3", b.CustomerName)
                    cmd.Parameters.AddWithValue("p4",
                        If(String.IsNullOrEmpty(b.CustomerPhone), CObj(DBNull.Value), CObj(b.CustomerPhone)))
                    cmd.Parameters.AddWithValue("p5", b.TicketCount)
                    cmd.Parameters.AddWithValue("p6", b.TotalAmount)
                    cmd.Parameters.AddWithValue("p7", b.Status.ToString())   ' plain VARCHAR – no ::cast needed
                    cmd.Parameters.AddWithValue("p8",
                        If(String.IsNullOrEmpty(b.Notes), CObj(DBNull.Value), CObj(b.Notes)))

                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tạo booking: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>
        ''' Cập nhật trạng thái booking.
        ''' Sử dụng Dapper một cách đồng nhất.
        ''' </summary>
        Public Sub UpdateStatus(id As Integer, status As BookingStatus)
            Const sql As String = "UPDATE bookings SET status = @Status WHERE id = @Id"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    conn.Execute(sql, New With {.Status = status.ToString(), .Id = id})
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi cập nhật trạng thái booking #{id}: {ex.Message}", ex)
            End Try
        End Sub

        ''' <summary>Returns number of assigned seats for a booking.</summary>
        Public Function GetAssignedSeatCount(bookingId As Integer) As Integer
            Const sql As String =
                "SELECT COUNT(1) FROM seat_assignments WHERE booking_id = @BookingId"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.ExecuteScalar(Of Integer)(sql, New With {.BookingId = bookingId})
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi đếm ghế đã gán: {ex.Message}", ex)
            End Try
        End Function

    End Class

End Namespace
