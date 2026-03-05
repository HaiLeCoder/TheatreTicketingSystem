Imports System.Drawing
Imports System.Windows.Forms

Namespace Forms

    ''' <summary>
    ''' Base form class that centralises shared UI helpers and messaging.
    ''' Loại bỏ các phương thức bổ trợ trùng lặp giữa frmPerformanceMaster,
    ''' frmBooking và frmSeatAssignment.
    ''' Các instance Font dùng chung được tạo một lần, giải phóng cùng form.
    ''' </summary>
    Public MustInherit Class BaseForm
        Inherits Form

        ' ── Các instance Font dùng chung (được tạo một lần, không tạo theo từng control) ──
        Protected ReadOnly FontNormal    As New Font("Segoe UI", 9.5F)
        Protected ReadOnly FontBold      As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Protected ReadOnly FontSmall     As New Font("Segoe UI", 8.0F)
        Protected ReadOnly FontLarge     As New Font("Segoe UI", 13.0F, FontStyle.Bold)

        ' ── Common dark-theme colours ──────────────────────────────────────────
        Protected ReadOnly ClrBackground  As Color = Color.FromArgb(22, 22, 38)
        Protected ReadOnly ClrPanelDark   As Color = Color.FromArgb(28, 28, 48)
        Protected ReadOnly ClrInput       As Color = Color.FromArgb(38, 38, 60)
        Protected ReadOnly ClrTextPrimary As Color = Color.FromArgb(210, 210, 230)
        Protected ReadOnly ClrTextMuted   As Color = Color.FromArgb(190, 190, 210)

        ' ── Label ─────────────────────────────────────────────────────────────

        Protected Function CreateLabel(text As String, x As Integer, y As Integer,
                                       Optional bold As Boolean = False,
                                       Optional size As Single = 9.5F) As Label
            Dim l       = New Label()
            l.Text      = text
            l.Location  = New Point(x, y)
            l.AutoSize  = True
            l.ForeColor = ClrTextMuted
            l.Font      = New Font("Segoe UI", size, If(bold, FontStyle.Bold, FontStyle.Regular))
            Return l
        End Function

        ' ── TextBox ───────────────────────────────────────────────────────────

        Protected Function CreateTextBox(x As Integer, y As Integer, w As Integer) As TextBox
            Dim t         = New TextBox()
            t.Location    = New Point(x, y)
            t.Size        = New Size(w, 28)
            t.BackColor   = ClrInput
            t.ForeColor   = ClrTextPrimary
            t.BorderStyle = BorderStyle.FixedSingle
            t.Font        = FontNormal
            Return t
        End Function

        ' ── DateTimePicker ────────────────────────────────────────────────────

        Protected Function CreateDtp(x As Integer, y As Integer,
                                     Optional w As Integer = 130) As DateTimePicker
            Dim d         = New DateTimePicker()
            d.Location    = New Point(x, y)
            d.Size        = New Size(w, 28)
            d.CalendarMonthBackground = ClrInput
            Return d
        End Function

        ' ── Button ────────────────────────────────────────────────────────────

        Protected Function CreateButton(text As String, x As Integer, y As Integer,
                                         w As Integer, h As Integer,
                                         backColor As Color) As Button
            Dim b                         = New Button()
            b.Text                        = text
            b.Location                    = New Point(x, y)
            b.Size                        = New Size(w, h)
            b.BackColor                   = backColor
            b.ForeColor                   = Color.White
            b.FlatStyle                   = FlatStyle.Flat
            b.FlatAppearance.BorderSize   = 0
            b.Font                        = FontBold
            b.Cursor                      = Cursors.Hand
            Return b
        End Function

        ' ── Panel ─────────────────────────────────────────────────────────────

        Protected Function CreatePanel(dock As DockStyle, height As Integer) As Panel
            Dim p       = New Panel()
            p.Dock      = dock
            If height > 0 Then p.Height = height
            p.BackColor = ClrBackground
            Return p
        End Function

        ' ── NumericUpDown styling ──────────────────────────────────────────────

        Protected Sub StyleNumeric(n As NumericUpDown)
            n.BackColor = ClrInput
            n.ForeColor = ClrTextPrimary
        End Sub

        ' ── DataGridView styling ───────────────────────────────────────────────

        Protected Sub StyleGrid(dgv As DataGridView)
            dgv.ReadOnly                  = True
            dgv.AllowUserToAddRows        = False
            dgv.AllowUserToDeleteRows     = False
            dgv.SelectionMode             = DataGridViewSelectionMode.FullRowSelect
            dgv.MultiSelect               = False
            dgv.BackgroundColor           = Color.FromArgb(28, 28, 45)
            dgv.GridColor                 = Color.FromArgb(50, 50, 70)
            dgv.BorderStyle               = BorderStyle.None
            dgv.RowHeadersVisible         = False
            dgv.AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 65)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 220)
            dgv.ColumnHeadersDefaultCellStyle.Font      = New Font("Segoe UI", 9, FontStyle.Bold)
            dgv.DefaultCellStyle.BackColor              = Color.FromArgb(28, 28, 45)
            dgv.DefaultCellStyle.ForeColor              = Color.FromArgb(210, 210, 230)
            dgv.DefaultCellStyle.SelectionBackColor     = Color.FromArgb(70, 50, 130)
            dgv.DefaultCellStyle.SelectionForeColor     = Color.White
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(33, 33, 52)
        End Sub

        ' ── Message helpers ───────────────────────────────────────────────────

        Protected Sub ShowSuccess(msg As String)
            MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Protected Sub ShowWarning(msg As String)
            MessageBox.Show(msg, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Sub

        Protected Sub ShowError(title As String, ex As Exception)
            MessageBox.Show($"{title}:{Environment.NewLine}{ex.Message}",
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Sub

        Protected Function Confirm(question As String,
                                   Optional title As String = "Xác nhận") As Boolean
            Return MessageBox.Show(question, title,
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Question) = DialogResult.Yes
        End Function

        ' ── Giải phóng các font dùng chung ──────────────────────────────────────

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                FontNormal.Dispose()
                FontBold.Dispose()
                FontSmall.Dispose()
                FontLarge.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

    End Class

End Namespace
