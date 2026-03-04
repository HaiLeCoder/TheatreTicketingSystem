Imports Npgsql
Imports System.Data

Namespace Infrastructure

    ''' <summary>
    ''' Database connection factory.
    ''' Provides open NpgsqlConnection instances using the centralised config.
    ''' All Repositories obtain connections through this factory.
    ''' </summary>
    Public NotInheritable Class DatabaseFactory

        Private Shared _instance As DatabaseFactory
        Private Shared ReadOnly _lock As New Object()

        Private ReadOnly _connectionString As String

        Private Sub New()
            _connectionString = AppConfiguration.Instance.ConnectionString
        End Sub

        ''' <summary>Returns the singleton instance (thread-safe).</summary>
        Public Shared ReadOnly Property Instance As DatabaseFactory
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New DatabaseFactory()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ''' <summary>
        ''' Creates and opens a new NpgsqlConnection.
        ''' Caller is responsible for disposing (use With … or Try/Finally).
        ''' </summary>
        Public Function CreateConnection() As NpgsqlConnection
            Dim conn As New NpgsqlConnection(_connectionString)
            conn.Open()
            Return conn
        End Function

        ''' <summary>
        ''' Tests the connection and returns True if successful.
        ''' Use during application startup to fail fast.
        ''' </summary>
        Public Function TestConnection() As Boolean
            Try
                Using conn = CreateConnection()
                    Return conn.State = ConnectionState.Open
                End Using
            Catch ex As Exception
                Return False
            End Try
        End Function

    End Class

End Namespace
