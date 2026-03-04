Imports System.Windows.Forms
Imports TheatreTicketingSystem.Forms

''' <summary>
''' Application entry point.
''' </summary>
Module Program

    <STAThread>
    Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.SetHighDpiMode(HighDpiMode.SystemAware)

        Application.Run(New frmMain())
    End Sub

End Module
