Imports Microsoft.Extensions.Configuration

Namespace Infrastructure

    ''' <summary>
    ''' Centralised application configuration.
    ''' Reads from appsettings.json – NEVER access raw environment variables directly.
    ''' Principle: Single Source of Truth for all config values.
    ''' RC-003 FIX: Removed C#-only ?? operator; replaced with VB.NET If() idiom.
    ''' </summary>
    Public NotInheritable Class AppConfiguration

        Private Shared _instance As AppConfiguration
        Private Shared ReadOnly _lock As New Object()

        Private ReadOnly _config As IConfiguration

        ''' <summary>Gets the PostgreSQL connection string.</summary>
        Public ReadOnly Property ConnectionString As String

        Private Sub New()
            Dim builder As New ConfigurationBuilder()
            builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            builder.AddJsonFile("appsettings.json", optional:=False, reloadOnChange:=False)
            _config = builder.Build()

            ' RC-003 FIX: VB.NET uses If() not ?? for null-coalescing
            Dim cs = _config.GetConnectionString("DefaultConnection")
            If cs Is Nothing OrElse cs.Trim().Length = 0 Then
                Throw New InvalidOperationException(
                    "Connection string 'DefaultConnection' not found in appsettings.json." &
                    Environment.NewLine &
                    "Please configure Host, Port, Database, Username and Password.")
            End If
            ConnectionString = cs
        End Sub

        ''' <summary>Returns the singleton instance (thread-safe).</summary>
        Public Shared ReadOnly Property Instance As AppConfiguration
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New AppConfiguration()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ''' <summary>Returns an arbitrary config value by key path.</summary>
        Public Function GetValue(key As String) As String
            ' RC-003 FIX: Use If() instead of ??
            Return If(_config(key), String.Empty)
        End Function

    End Class

End Namespace
