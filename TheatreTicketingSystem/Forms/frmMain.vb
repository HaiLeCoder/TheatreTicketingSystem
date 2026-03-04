Imports System.Drawing
Imports System.Windows.Forms
Imports TheatreTicketingSystem.Infrastructure

Namespace Forms

    ''' <summary>
    ''' Application entry point form.
    ''' Acts as navigation hub (MDI-style or tabbed navigation).
    ''' </summary>
    Public Class frmMain
        Inherits BaseForm   ' RC-016: Inherit BaseForm

        ' ── Controls ──────────────────────────────────────────────────────────
        Private WithEvents btnPerformances As Button
        Private WithEvents btnBooking      As Button
        Private WithEvents btnSeatAssign   As Button
        Private WithEvents btnExit         As Button
        Private pnlSidebar  As Panel
        Private pnlContent  As Panel
        Private lblTitle    As Label
        Private lblVersion  As Label
        Private picLogo     As PictureBox

        Public Sub New()
            InitializeComponent()
            CheckDatabaseConnection()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()

            Me.Text                 = "🎭 Theatre Ticketing System"
            Me.Size                 = New Size(1200, 750)
            Me.StartPosition        = FormStartPosition.CenterScreen
            Me.MinimumSize          = New Size(900, 600)
            Me.BackColor            = Color.FromArgb(18, 18, 30)
            Me.Font                 = New Font("Segoe UI", 9.5F)
            Me.IsMdiContainer       = False

            BuildSidebar()
            BuildContentArea()

            Me.ResumeLayout(False)
        End Sub

        ' ── Sidebar ───────────────────────────────────────────────────────────

        Private Sub BuildSidebar()
            pnlSidebar              = New Panel()
            pnlSidebar.Dock         = DockStyle.Left
            pnlSidebar.Width        = 240
            pnlSidebar.BackColor    = Color.FromArgb(24, 24, 40)

            ' Title
            lblTitle                = New Label()
            lblTitle.Text           = "🎭 Theatre" & Environment.NewLine & "   Ticketing"
            lblTitle.Font           = New Font("Segoe UI", 16, FontStyle.Bold)
            lblTitle.ForeColor      = Color.FromArgb(200, 150, 255)
            lblTitle.Location       = New Point(20, 30)
            lblTitle.Size           = New Size(200, 70)
            pnlSidebar.Controls.Add(lblTitle)

            ' Divider
            Dim divider             = New Panel()
            divider.Location        = New Point(20, 110)
            divider.Size            = New Size(200, 1)
            divider.BackColor       = Color.FromArgb(60, 60, 80)
            pnlSidebar.Controls.Add(divider)

            ' Nav buttons
            btnPerformances         = CreateNavButton("🎪  Suất diễn", 130)
            btnBooking              = CreateNavButton("🎟️  Đặt vé", 200)
            btnSeatAssign           = CreateNavButton("🪑  Gán ghế", 270)

            pnlSidebar.Controls.AddRange({btnPerformances, btnBooking, btnSeatAssign})

            ' Version label
            lblVersion              = New Label()
            lblVersion.Text         = "v1.0.0 | © 2026 Theatre System"
            lblVersion.ForeColor    = Color.FromArgb(100, 100, 130)
            lblVersion.Font         = New Font("Segoe UI", 7.5F)
            lblVersion.Dock         = DockStyle.Bottom
            lblVersion.Height       = 30
            lblVersion.TextAlign    = ContentAlignment.MiddleCenter
            pnlSidebar.Controls.Add(lblVersion)

            ' Exit button
            btnExit                 = CreateNavButton("✕  Thoát", 0)
            btnExit.Dock            = DockStyle.Bottom
            btnExit.Height          = 45
            btnExit.BackColor       = Color.FromArgb(140, 30, 50)
            pnlSidebar.Controls.Add(btnExit)

            Me.Controls.Add(pnlSidebar)
        End Sub

        Private Function CreateNavButton(text As String, top As Integer) As Button
            Dim btn         = New Button()
            btn.Text        = text
            btn.Location    = New Point(10, top)
            btn.Size        = New Size(220, 55)
            btn.FlatStyle   = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 90)
            btn.BackColor   = Color.Transparent
            btn.ForeColor   = Color.FromArgb(220, 220, 240)
            btn.Font        = New Font("Segoe UI", 11, FontStyle.Regular)
            btn.TextAlign   = ContentAlignment.MiddleLeft
            btn.Padding     = New Padding(10, 0, 0, 0)
            btn.Cursor      = Cursors.Hand
            Return btn
        End Function

        ' ── Content panel ─────────────────────────────────────────────────────

        Private Sub BuildContentArea()
            pnlContent              = New Panel()
            pnlContent.Dock         = DockStyle.Fill
            pnlContent.BackColor    = Color.FromArgb(18, 18, 30)
            Me.Controls.Add(pnlContent)
            pnlContent.BringToFront()

            ShowWelcome()
        End Sub

        Private Sub ShowWelcome()
            pnlContent.Controls.Clear()

            Dim lbl             = New Label()
            lbl.Text            = "Chào mừng đến với Theatre Ticketing System" &
                                  Environment.NewLine & Environment.NewLine &
                                  "→  Chọn chức năng từ menu bên trái để bắt đầu."
            lbl.ForeColor       = Color.FromArgb(160, 160, 200)
            lbl.Font            = New Font("Segoe UI", 13)
            lbl.AutoSize        = False
            lbl.Size            = New Size(700, 200)
            lbl.Location        = New Point(100, 200)
            lbl.TextAlign       = ContentAlignment.MiddleLeft
            pnlContent.Controls.Add(lbl)
        End Sub

        Private Sub LoadChildForm(childForm As Form)
            ' RC-018 FIX: Dispose previous child forms to prevent GDI/memory leaks
            For Each ctrl In pnlContent.Controls.Cast(Of Control)().ToList()
                ctrl.Dispose()
            Next
            pnlContent.Controls.Clear()

            childForm.TopLevel        = False
            childForm.FormBorderStyle = FormBorderStyle.None
            childForm.Dock            = DockStyle.Fill
            pnlContent.Controls.Add(childForm)
            childForm.Show()
        End Sub

        ' ── Event handlers ────────────────────────────────────────────────────

        Private Sub btnPerformances_Click(sender As Object, e As EventArgs) Handles btnPerformances.Click
            LoadChildForm(New frmPerformanceMaster())
        End Sub

        Private Sub btnBooking_Click(sender As Object, e As EventArgs) Handles btnBooking.Click
            LoadChildForm(New frmBooking())
        End Sub

        Private Sub btnSeatAssign_Click(sender As Object, e As EventArgs) Handles btnSeatAssign.Click
            LoadChildForm(New frmSeatAssignment())
        End Sub

        Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
            If MessageBox.Show("Bạn có chắc muốn thoát?", "Xác nhận thoát",
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Application.Exit()
            End If
        End Sub

        ' ── Database connectivity check ────────────────────────────────────

        Private Sub CheckDatabaseConnection()
            If Not DatabaseFactory.Instance.TestConnection() Then
                MessageBox.Show(
                    "⚠️  Không thể kết nối PostgreSQL." & Environment.NewLine &
                    "Vui lòng kiểm tra cấu hình trong appsettings.json." & Environment.NewLine & Environment.NewLine &
                    "Chương trình sẽ tiếp tục nhưng các thao tác dữ liệu sẽ thất bại.",
                    "Lỗi kết nối Database",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
            End If
        End Sub

    End Class

End Namespace
