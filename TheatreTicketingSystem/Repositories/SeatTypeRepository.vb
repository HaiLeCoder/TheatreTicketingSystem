Imports Dapper
Imports Npgsql
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Infrastructure
Imports TheatreTicketingSystem.Utils

Namespace Repositories

    ''' <summary>
    ''' Repository for the "seat_types" lookup table.
    ''' RC-009 FIX: Added NpgsqlException handling.
    ''' RC-013 FIX: In-memory cache now expires after CACHE_TTL_MINUTES.
    ''' </summary>
    Public Class SeatTypeRepository

        Private Shared _instance As SeatTypeRepository
        Private Shared ReadOnly _lock As New Object()

        Private _cachedTypes As List(Of SeatType)
        ' RC-013 FIX: Track when the cache was populated
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
        ''' Returns all seat types.
        ''' RC-013 FIX: Cache expires after 60 minutes so DB changes are eventually reflected.
        ''' </summary>
        Public Function GetAll() As List(Of SeatType)
            ' RC-013 FIX: Check TTL before returning cached data
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
