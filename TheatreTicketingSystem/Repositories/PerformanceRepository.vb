Imports Dapper
Imports Npgsql
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Infrastructure
Imports TheatreTicketingSystem.Utils

Namespace Repositories

    ''' <summary>
    ''' Repository for CRUD operations on the "performances" table.
    ''' RC-009 FIX: All public methods now wrap NpgsqlException in DataAccessException.
    ''' All SQL is parameterised to prevent SQL injection.
    ''' </summary>
    Public Class PerformanceRepository

        Private Shared _instance As PerformanceRepository
        Private Shared ReadOnly _lock As New Object()

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property Instance As PerformanceRepository
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New PerformanceRepository()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ' ── Shared SELECT projection ───────────────────────────────────────────

        Private Const BASE_PERF_SELECT As String =
            "SELECT id, name, start_time AS StartTime, duration_minutes AS DurationMinutes," &
            "       location, description, total_seats AS TotalSeats, is_active AS IsActive," &
            "       created_at AS CreatedAt, updated_at AS UpdatedAt " &
            "FROM performances "

        ' ── Queries ───────────────────────────────────────────────────────────

        ''' <summary>Retrieves all active performances ordered by start time.</summary>
        Public Function GetAll() As List(Of Performance)
            Dim sql = BASE_PERF_SELECT & "WHERE is_active = TRUE ORDER BY start_time ASC"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.Query(Of Performance)(sql).AsList()
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải danh sách suất diễn: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>Returns a single performance by ID. Returns Nothing if not found.</summary>
        Public Function GetById(id As Integer) As Performance
            Dim sql = BASE_PERF_SELECT & "WHERE id = @Id"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.QuerySingleOrDefault(Of Performance)(sql, New With {.Id = id})
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải suất diễn #{id}: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>Searches performances by name and/or date range.</summary>
        Public Function Search(nameFilter As String,
                               fromDate As DateTime?,
                               toDate As DateTime?) As List(Of Performance)
            Dim sql As New System.Text.StringBuilder(BASE_PERF_SELECT)
            sql.Append("WHERE is_active = TRUE")

            Dim params As New DynamicParameters()

            If Not String.IsNullOrWhiteSpace(nameFilter) Then
                sql.Append(" AND name ILIKE @Name")
                params.Add("Name", $"%{nameFilter.Trim()}%")
            End If

            If fromDate.HasValue Then
                sql.Append(" AND start_time >= @FromDate")
                ' RC-021: Explicitly handle DateTimeKind for Npgsql compatibility
                params.Add("FromDate", fromDate.Value.ToUniversalTime())
            End If

            If toDate.HasValue Then
                sql.Append(" AND start_time <= @ToDate")
                params.Add("ToDate", toDate.Value.ToUniversalTime())
            End If

            sql.Append(" ORDER BY start_time ASC")

            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.Query(Of Performance)(sql.ToString(), params).AsList()
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tìm kiếm suất diễn: {ex.Message}", ex)
            End Try
        End Function

        ' ── Commands ──────────────────────────────────────────────────────────

        ''' <summary>Inserts a new performance and returns the generated ID.</summary>
        Public Function Create(p As Performance) As Integer
            Const sql As String =
                "INSERT INTO performances " &
                "  (name, start_time, duration_minutes, location, description, total_seats) " &
                "VALUES (@Name, @StartTime, @DurationMinutes, @Location, @Description, @TotalSeats) " &
                "RETURNING id"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.ExecuteScalar(Of Integer)(sql, New With {
                        p.Name, p.StartTime, p.DurationMinutes,
                        p.Location, p.Description, p.TotalSeats
                    })
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tạo suất diễn: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>Updates an existing performance.</summary>
        Public Sub Update(p As Performance)
            Const sql As String =
                "UPDATE performances " &
                "SET name = @Name, start_time = @StartTime, " &
                "    duration_minutes = @DurationMinutes, location = @Location, " &
                "    description = @Description, total_seats = @TotalSeats " &
                "WHERE id = @Id"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    conn.Execute(sql, New With {
                        p.Id, p.Name, p.StartTime, p.DurationMinutes,
                        p.Location, p.Description, p.TotalSeats
                    })
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi cập nhật suất diễn: {ex.Message}", ex)
            End Try
        End Sub

        ''' <summary>Soft-deletes a performance (sets is_active = FALSE).</summary>
        Public Sub Delete(id As Integer)
            Const sql As String = "UPDATE performances SET is_active = FALSE WHERE id = @Id"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    conn.Execute(sql, New With {.Id = id})
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi xóa suất diễn #{id}: {ex.Message}", ex)
            End Try
        End Sub

        ''' <summary>Returns True if any active booking exists for this performance.</summary>
        Public Function HasBookings(performanceId As Integer) As Boolean
            Const sql As String =
                "SELECT COUNT(1) FROM bookings " &
                "WHERE performance_id = @PerformanceId AND status <> 'CANCELLED'"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    Return conn.ExecuteScalar(Of Integer)(sql,
                        New With {.PerformanceId = performanceId}) > 0
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi kiểm tra booking của suất diễn: {ex.Message}", ex)
            End Try
        End Function

    End Class

End Namespace
