Imports Dapper
Imports Npgsql
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Infrastructure
Imports TheatreTicketingSystem.Utils

Namespace Repositories

    ''' <summary>
    ''' Repository for seat assignment operations.
    ''' RC-005 FIX: SaveAssignments now catches NpgsqlException SqlState 23505
    '''             (unique violation) and converts to friendly SeatConflictException.
    ''' RC-009 FIX: All public methods wrapped with NpgsqlException handling.
    ''' </summary>
    Public Class SeatAssignmentRepository

        Private Shared _instance As SeatAssignmentRepository
        Private Shared ReadOnly _lock As New Object()

        ' PostgreSQL unique-violation SQLSTATE code
        Private Const SQLSTATE_UNIQUE_VIOLATION As String = "23505"

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property Instance As SeatAssignmentRepository
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New SeatAssignmentRepository()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ' ── Queries ───────────────────────────────────────────────────────────

        ''' <summary>Returns all seat assignments for a given performance.</summary>
        Public Function GetByPerformance(performanceId As Integer) As List(Of SeatAssignment)
            Const sql As String =
                "SELECT id, booking_id AS BookingId, performance_id AS PerformanceId," &
                "       row_label AS RowLabel, col_number AS ColNumber, assigned_at AS AssignedAt " &
                "FROM seat_assignments WHERE performance_id = @PerformanceId"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.Query(Of SeatAssignment)(sql,
                        New With {.PerformanceId = performanceId}).AsList()
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải sơ đồ ghế: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>Returns all seat assignments for a booking.</summary>
        Public Function GetByBooking(bookingId As Integer) As List(Of SeatAssignment)
            Const sql As String =
                "SELECT id, booking_id AS BookingId, performance_id AS PerformanceId," &
                "       row_label AS RowLabel, col_number AS ColNumber, assigned_at AS AssignedAt " &
                "FROM seat_assignments " &
                "WHERE booking_id = @BookingId ORDER BY row_label, col_number"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.Query(Of SeatAssignment)(sql,
                        New With {.BookingId = bookingId}).AsList()
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải ghế của booking: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>
        ''' Returns a HashSet of "RowCol" keys (e.g. "A5") already taken
        ''' in a performance, excluding the given booking.
        ''' </summary>
        Public Function GetOccupiedSeats(performanceId As Integer,
                                          excludeBookingId As Integer) As HashSet(Of String)
            Const sql As String =
                "SELECT row_label || col_number::TEXT FROM seat_assignments " &
                "WHERE performance_id = @PerformanceId AND booking_id <> @ExcludeBookingId"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Dim keys = conn.Query(Of String)(sql, New With {
                        .PerformanceId    = performanceId,
                        .ExcludeBookingId = excludeBookingId
                    })
                    Return New HashSet(Of String)(keys)
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi kiểm tra ghế đã đặt: {ex.Message}", ex)
            End Try
        End Function

        ' ── Commands ──────────────────────────────────────────────────────────

        ''' <summary>
        ''' Saves seat assignments atomically (DELETE existing + INSERT new) in one transaction.
        ''' RC-005 FIX: Catches unique-constraint violation (SQLSTATE 23505) inside the
        '''             transaction and throws SeatConflictException with a clear message,
        '''             instead of leaking raw NpgsqlException to the UI.
        ''' </summary>
        Public Sub SaveAssignments(bookingId As Integer,
                                   performanceId As Integer,
                                   seats As List(Of SeatAssignment))
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Using tx = conn.BeginTransaction()
                        Try
                            ' 1. Clear previous assignments for this booking
                            Dim delCmd = conn.CreateCommand()
                            delCmd.Transaction  = tx
                            delCmd.CommandText  = "DELETE FROM seat_assignments WHERE booking_id = @p1"
                            delCmd.Parameters.AddWithValue("p1", bookingId)
                            delCmd.ExecuteNonQuery()

                            ' 2. Insert new assignments one by one
                            For Each seat In seats
                                Dim insCmd = conn.CreateCommand()
                                insCmd.Transaction = tx
                                insCmd.CommandText =
                                    "INSERT INTO seat_assignments " &
                                    "  (booking_id, performance_id, row_label, col_number) " &
                                    "VALUES (@p1, @p2, @p3, @p4)"
                                insCmd.Parameters.AddWithValue("p1", bookingId)
                                insCmd.Parameters.AddWithValue("p2", performanceId)
                                insCmd.Parameters.AddWithValue("p3", seat.RowLabel.ToString())
                                insCmd.Parameters.AddWithValue("p4", seat.ColNumber)
                                insCmd.ExecuteNonQuery()
                            Next

                            tx.Commit()

                        Catch ex As NpgsqlException When ex.SqlState = SQLSTATE_UNIQUE_VIOLATION
                            ' RC-005 FIX: Rollback & raise friendly error instead of crashing
                            tx.Rollback()
                            Throw New SeatConflictException(
                                "Một hoặc nhiều ghế bạn chọn vừa được booking khác đặt trước." &
                                Environment.NewLine &
                                "Vui lòng tải lại sơ đồ ghế và chọn lại.", ex)

                        Catch
                            tx.Rollback()
                            Throw
                        End Try
                    End Using
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi kết nối khi lưu ghế: {ex.Message}", ex)
            End Try
        End Sub

    End Class

End Namespace
