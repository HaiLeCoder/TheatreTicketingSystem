Imports Dapper
Imports Npgsql
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Infrastructure
Imports TheatreTicketingSystem.Utils

Namespace Repositories

    ''' <summary>
    ''' Repository cho bảng tra cứu "seat_types".
    ''' Đã thêm xử lý ngoại lệ Npgsql.
    ''' Bộ nhớ đệm in-memory hiện hết hạn sau CACHE_TTL_MINUTES.
    ''' </summary>
    Public Class SeatTypeRepository

        Private Shared _instance As SeatTypeRepository
        Private Shared ReadOnly _lock As New Object()

        Private _cachedTypes As List(Of SeatType)
        ' Theo dõi thời điểm bộ nhớ đệm được điền dữ liệu
        Private _cacheLoadedAt As DateTime = DateTime.MinValue
        Private Const CACHE_TTL_MINUTES As Integer = 60

        Private Sub New()
        End Sub

        Public Shared ReadOnly Property Instance As SeatTypeRepository
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New SeatTypeRepository()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ''' <summary>
        ''' Bộ nhớ đệm hết hạn sau 60 phút để đảm bảo các thay đổi trong DB được phản ánh.
        ''' </summary>
        Public Function GetAll() As List(Of SeatType)
            ' Kiểm tra TTL trước khi trả về dữ liệu lưu trong bộ nhớ đệm
            Dim cacheAge = (DateTime.Now - _cacheLoadedAt).TotalMinutes
            If _cachedTypes IsNot Nothing AndAlso cacheAge < CACHE_TTL_MINUTES Then
                Return _cachedTypes
            End If

            Const sql As String =
                "SELECT id, name, price, description, created_at AS CreatedAt " &
                "FROM seat_types ORDER BY price ASC"
            Try
                Using conn = DatabaseFactory.Instance.CreateConnection()
                    _cachedTypes    = conn.Query(Of SeatType)(sql).AsList()
                    _cacheLoadedAt  = DateTime.Now
                    Return _cachedTypes
                End Using
            Catch ex As NpgsqlException
                Throw New DataAccessException($"Lỗi tải loại ghế: {ex.Message}", ex)
            End Try
        End Function

        ''' <summary>Returns a seat type by ID (uses cached list).</summary>
        Public Function GetById(id As Integer) As SeatType
            Return GetAll().FirstOrDefault(Function(st) st.Id = id)
        End Function

        ''' <summary>Forces the cache to refresh on next call.</summary>
        Public Sub InvalidateCache()
            _cachedTypes   = Nothing
            _cacheLoadedAt = DateTime.MinValue
        End Sub

    End Class

End Namespace
