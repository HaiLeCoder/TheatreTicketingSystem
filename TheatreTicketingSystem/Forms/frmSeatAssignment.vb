Imports System.Drawing
Imports System.Windows.Forms
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Services
Imports TheatreTicketingSystem.Utils

Namespace Forms

    ''' <summary>
    ''' frmSeatAssignment – Chức năng 3: Gán ghế theo sơ đồ 10×10
    ''' Đã xử lý bắt lỗi SeatConflictException riêng biệt với thông báo thân thiện.
    ''' Kế thừa từ BaseForm.
    ''' </summary>
    Public Class frmSeatAssignment
        Inherits BaseForm

        Private ReadOnly _seatService    As SeatAssignmentService = SeatAssignmentService.Instance
        Private ReadOnly _bookingService As BookingService        = BookingService.Instance

        Private _currentBooking  As Booking = Nothing
        Private _seatMap         As Dictionary(Of String, Integer)
        Private _selectedSeats   As New List(Of String)
        Private _seatButtons     As New Dictionary(Of String, Button)

        Private Shared ReadOnly CLR_FREE     As Color = Color.White
        Private Shared ReadOnly CLR_TAKEN    As Color = Color.Red
        Private Shared ReadOnly CLR_SELECTED As Color = Color.Blue

        Private pnlTop      As Panel
        Private pnlSeatGrid As Panel
        Private pnlInfo     As Panel
        Private pnlLegend   As Panel

        Private txtBookingId  As TextBox
        Private WithEvents btnLoad  As Button
        Private WithEvents btnSave  As Button
        Private WithEvents btnReset As Button

        Private lblBookingInfo  As Label
        Private lblSeatCounter  As Label
        Private dgvAssigned     As DataGridView

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.BackColor = ClrBackground
            Me.ForeColor = ClrTextPrimary
            Me.Font      = FontNormal
            Me.Padding   = New Padding(15)
            BuildTopPanel()
            BuildLegend()
            BuildSeatGrid()
            BuildInfoPanel()
            Me.ResumeLayout(False)
        End Sub

        ' ── Top Panel ─────────────────────────────────────────────────────────

        Private Sub BuildTopPanel()
            pnlTop           = New Panel()
            pnlTop.Dock      = DockStyle.Top
            pnlTop.Height    = 70
            pnlTop.BackColor = ClrBackground

            Dim lbl = CreateLabel("🪑  GÁN GHẾ THEO SƠ ĐỒ", 0, 8, bold:=True, size:=14)
            lbl.ForeColor = Color.FromArgb(255, 180, 80)
            pnlTop.Controls.Add(lbl)

            Dim lblId = CreateLabel("Booking ID:", 0, 45)
            pnlTop.Controls.Add(lblId)

            txtBookingId             = New TextBox()
            txtBookingId.Location    = New Point(95, 42)
            txtBookingId.Size        = New Size(100, 28)
            txtBookingId.BackColor   = ClrInput
            txtBookingId.ForeColor   = ClrTextPrimary
            txtBookingId.BorderStyle = BorderStyle.FixedSingle
            pnlTop.Controls.Add(txtBookingId)

            btnLoad  = CreateButton("📂 Tải",    215, 41, 90, 30, Color.FromArgb(50, 90, 160))
            btnSave  = CreateButton("💾 Lưu ghế", 315, 41, 120, 30, Color.FromArgb(30, 120, 70))
            btnReset = CreateButton("↺ Bỏ chọn", 445, 41, 110, 30, Color.FromArgb(80, 60, 100))

            btnSave.Enabled  = False
            btnReset.Enabled = False

            pnlTop.Controls.AddRange({btnLoad, btnSave, btnReset})
            Me.Controls.Add(pnlTop)
        End Sub

        ' ── Legend ────────────────────────────────────────────────────────────

        Private Sub BuildLegend()
            pnlLegend           = New Panel()
            pnlLegend.Location  = New Point(15, 95)
            pnlLegend.Size      = New Size(620, 35)
            pnlLegend.BackColor = ClrBackground

            Dim items As (Clr As Color, Txt As String)() = {
                (CLR_FREE,     "Trống"),
                (CLR_SELECTED, "Đang chọn"),
                (CLR_TAKEN,    "Đã đặt")
            }
            Dim x = 0
            For Each item In items
                Dim sq      = New Panel()
                sq.Location = New Point(x, 8)
                sq.Size     = New Size(16, 16)
                sq.BackColor = item.Clr
                pnlLegend.Controls.Add(sq)
                pnlLegend.Controls.Add(CreateLabel(item.Txt, x + 20, 8))
                x += 130
            Next
            Me.Controls.Add(pnlLegend)
        End Sub

        ' ── Seat Grid ─────────────────────────────────────────────────────────

        Private Sub BuildSeatGrid()
            pnlSeatGrid            = New Panel()
            pnlSeatGrid.Location   = New Point(15, 135)
            pnlSeatGrid.Size       = New Size(620, 580)
            pnlSeatGrid.Anchor     = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
            pnlSeatGrid.AutoScroll = True
            pnlSeatGrid.BackColor  = ClrBackground

            Dim lblStage           = New Label()
            lblStage.Text          = "▬▬▬▬▬  SÂN KHẤU  ▬▬▬▬▬"
            lblStage.Font          = New Font("Segoe UI", 10, FontStyle.Bold)
            lblStage.ForeColor     = Color.FromArgb(255, 200, 80)
            lblStage.AutoSize      = True
            lblStage.Location      = New Point(115, 5)
            pnlSeatGrid.Controls.Add(lblStage)

            For c = 1 To 10
                Dim lbl         = New Label()
                lbl.Text        = c.ToString()
                lbl.Location    = New Point(55 + (c - 1) * 52, 35)
                lbl.Size        = New Size(48, 20)
                lbl.TextAlign   = ContentAlignment.MiddleCenter
                lbl.ForeColor   = Color.FromArgb(150, 150, 180)
                lbl.Font        = FontSmall
                pnlSeatGrid.Controls.Add(lbl)
            Next

            _seatButtons.Clear()
            For r = 0 To 9
                Dim rowChar = Chr(Asc("A"c) + r)
                Dim lblRow  = New Label()
                lblRow.Text     = rowChar
                lblRow.Location = New Point(30, 58 + r * 52)
                lblRow.Size     = New Size(22, 46)
                lblRow.TextAlign = ContentAlignment.MiddleCenter
                lblRow.ForeColor = Color.FromArgb(180, 180, 200)
                lblRow.Font     = FontBold
                pnlSeatGrid.Controls.Add(lblRow)

                For c = 1 To 10
                    Dim key   = $"{rowChar}{c}"
                    Dim btn   = New Button()
                    btn.Name  = key
                    btn.Text  = key
                    btn.Size  = New Size(46, 46)
                    btn.Location = New Point(55 + (c - 1) * 52, 58 + r * 52)
                    btn.FlatStyle = FlatStyle.Flat
                    btn.FlatAppearance.BorderSize  = 1
                    btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 90)
                    btn.BackColor = CLR_FREE
                    btn.ForeColor = Color.Black
                    btn.Font    = FontSmall
                    btn.Tag     = key
                    btn.Cursor  = Cursors.Hand
                    btn.Enabled = False
                    AddHandler btn.Click, AddressOf SeatButton_Click
                    _seatButtons(key) = btn
                    pnlSeatGrid.Controls.Add(btn)
                Next
            Next
            Me.Controls.Add(pnlSeatGrid)
        End Sub

        ' ── Info Panel ────────────────────────────────────────────────────────

        Private Sub BuildInfoPanel()
            pnlInfo           = New Panel()
            pnlInfo.Location  = New Point(648, 75)
            pnlInfo.Size      = New Size(380, 640)
            pnlInfo.Anchor    = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Right
            pnlInfo.BackColor = ClrPanelDark
            pnlInfo.Padding   = New Padding(15)

            Dim lblTitle      = CreateLabel("Thông tin booking", 15, 12, bold:=True, size:=11)
            lblTitle.ForeColor = Color.FromArgb(255, 180, 80)
            pnlInfo.Controls.Add(lblTitle)

            lblBookingInfo          = New Label()
            lblBookingInfo.Location = New Point(15, 40)
            lblBookingInfo.Size     = New Size(350, 140)
            lblBookingInfo.ForeColor = Color.FromArgb(185, 185, 210)
            lblBookingInfo.Font     = FontNormal
            lblBookingInfo.Text     = "(Chưa tải booking)"
            pnlInfo.Controls.Add(lblBookingInfo)

            Dim div       = New Panel()
            div.Location  = New Point(15, 188)
            div.Size      = New Size(350, 1)
            div.BackColor = Color.FromArgb(60, 60, 80)
            pnlInfo.Controls.Add(div)

            lblSeatCounter          = New Label()
            lblSeatCounter.Location = New Point(15, 198)
            lblSeatCounter.Size     = New Size(350, 35)
            lblSeatCounter.Font     = New Font("Segoe UI", 11, FontStyle.Bold)
            lblSeatCounter.ForeColor = Color.FromArgb(80, 220, 150)
            lblSeatCounter.Text     = "Đã chọn: 0 / 0 ghế"
            pnlInfo.Controls.Add(lblSeatCounter)

            pnlInfo.Controls.Add(CreateLabel("Ghế đã gán:", 15, 242, bold:=True))

            dgvAssigned              = New DataGridView()
            dgvAssigned.Location     = New Point(15, 265)
            dgvAssigned.Size         = New Size(350, 355)
            dgvAssigned.Anchor       = AnchorStyles.Top Or AnchorStyles.Bottom
            StyleGrid(dgvAssigned)   ' Sử dụng helper từ BaseForm

            Dim colSeat As New DataGridViewTextBoxColumn()
            colSeat.HeaderText = "Ghế" : colSeat.FillWeight = 50
            Dim colStatus As New DataGridViewTextBoxColumn()
            colStatus.HeaderText = "Trạng thái" : colStatus.FillWeight = 50
            dgvAssigned.Columns.AddRange({colSeat, colStatus})

            pnlInfo.Controls.Add(dgvAssigned)
            Me.Controls.Add(pnlInfo)
        End Sub

        ' ── Load Booking ──────────────────────────────────────────────────────

        Private Sub LoadBooking()
            ' Parse một lần, sử dụng biến cục bộ bookingId ở mọi nơi
            Dim bookingId As Integer
            If Not Integer.TryParse(txtBookingId.Text.Trim(), bookingId) OrElse bookingId <= 0 Then
                ShowWarning("Vui lòng nhập Booking ID hợp lệ (số nguyên dương).")
                Return
            End If

            Try
                _currentBooking = _bookingService.GetById(bookingId)
            Catch ex As KeyNotFoundException
                ShowWarning(ex.Message)
                Return
            Catch ex As Exception
                ShowError("Lỗi tải booking", ex)
                Return
            End Try

            Try
                _seatMap = _seatService.GetSeatMap(_currentBooking.PerformanceId, bookingId)
                Dim alreadyAssigned = _seatService.GetAssignedSeats(bookingId)
                _selectedSeats.Clear()
                For Each sa In alreadyAssigned
                    _selectedSeats.Add(sa.SeatLabel)
                Next
                RefreshSeatUI()
                UpdateBookingInfo()
                UpdateSeatCounter()
                LoadAssignedGrid()
                btnSave.Enabled  = True
                btnReset.Enabled = True
            Catch ex As Exception
                ShowError("Lỗi tải sơ đồ ghế", ex)
            End Try
        End Sub

        Private Sub RefreshSeatUI()
            For Each kvp In _seatButtons
                Dim key = kvp.Key
                Dim btn = kvp.Value
                btn.Enabled = True
                If _selectedSeats.Contains(key) Then
                    btn.BackColor = CLR_SELECTED
                    btn.ForeColor = Color.White
                ElseIf _seatMap.ContainsKey(key) AndAlso _seatMap(key) <> 0 Then
                    btn.BackColor = CLR_TAKEN
                    btn.ForeColor = Color.White
                    btn.Enabled   = False
                Else
                    btn.BackColor = CLR_FREE
                    btn.ForeColor = Color.Black
                End If
            Next
        End Sub

        Private Sub UpdateBookingInfo()
            If _currentBooking Is Nothing Then
                lblBookingInfo.Text = "(Chưa tải booking)"
                Return
            End If
            lblBookingInfo.Text =
                $"Booking  #: {_currentBooking.Id}" & Environment.NewLine &
                $"Suất diễn : {_currentBooking.PerformanceName}" & Environment.NewLine &
                $"Khách hàng: {_currentBooking.CustomerName}" & Environment.NewLine &
                $"Loại ghế  : {_currentBooking.SeatTypeName}" & Environment.NewLine &
                $"Số vé     : {_currentBooking.TicketCount}" & Environment.NewLine &
                $"Tổng tiền : {_currentBooking.TotalAmount:N0} VNĐ" & Environment.NewLine &
                $"Trạng thái: {_currentBooking.StatusDisplay}"
        End Sub

        Private Sub UpdateSeatCounter()
            Dim max = If(_currentBooking IsNot Nothing, _currentBooking.TicketCount, 0)
            lblSeatCounter.Text = $"Đã chọn: {_selectedSeats.Count} / {max} ghế"
            lblSeatCounter.ForeColor = If(_selectedSeats.Count = max AndAlso max > 0,
                                          Color.FromArgb(80, 220, 100),
                                          Color.FromArgb(255, 180, 70))
        End Sub

        Private Sub LoadAssignedGrid()
            dgvAssigned.Rows.Clear()
            For Each key In _selectedSeats
                dgvAssigned.Rows.Add(key, "✔ Đang chọn")
            Next
        End Sub

        ' ── Seat Button Click ─────────────────────────────────────────────────

        Private Sub SeatButton_Click(sender As Object, e As EventArgs)
            If _currentBooking Is Nothing Then Return
            Dim btn  = CType(sender, Button)
            Dim key  = CStr(btn.Tag)
            Dim maxS = _currentBooking.TicketCount
            If _selectedSeats.Contains(key) Then
                _selectedSeats.Remove(key)
                btn.BackColor = CLR_FREE
                btn.ForeColor = Color.Black
            Else
                If _selectedSeats.Count >= maxS Then
                    ShowWarning($"Chỉ được chọn tối đa {maxS} ghế cho booking này.")
                    Return
                End If
                _selectedSeats.Add(key)
                btn.BackColor = CLR_SELECTED
                btn.ForeColor = Color.White
            End If
            UpdateSeatCounter()
            LoadAssignedGrid()
        End Sub

        ' ── Save ──────────────────────────────────────────────────────────────

        Private Sub SaveAssignments()
            If _currentBooking Is Nothing Then
                ShowWarning("Vui lòng tải booking trước.")
                Return
            End If
            Dim seats = _selectedSeats.Select(Function(key)
                Return New SeatAssignment With {
                    .RowLabel  = key(0),
                    .ColNumber = Integer.Parse(key.Substring(1))
                }
            End Function).ToList()

            Try
                _seatService.SaveAssignments(_currentBooking.Id, seats)
                ShowSuccess($"Đã lưu {seats.Count} ghế cho Booking #{_currentBooking.Id}!")
                LoadBooking()
            Catch ex As InvalidOperationException
                ShowWarning(ex.Message)
            Catch ex As SeatConflictException
                ' Thông báo thân thiện cho xung đột ghế (race-condition)
                ShowWarning(ex.Message)
            Catch ex As Exception
                ShowError("Lỗi lưu ghế", ex)
            End Try
        End Sub

        ' ── Events ────────────────────────────────────────────────────────────

        Private Sub btnLoad_Click(s As Object, e As EventArgs) Handles btnLoad.Click
            LoadBooking()
        End Sub

        Private Sub btnSave_Click(s As Object, e As EventArgs) Handles btnSave.Click
            If Confirm("Lưu các ghế đã chọn?", "Xác nhận lưu") Then
                SaveAssignments()
            End If
        End Sub

        Private Sub btnReset_Click(s As Object, e As EventArgs) Handles btnReset.Click
            _selectedSeats.Clear()
            RefreshSeatUI()
            UpdateSeatCounter()
            LoadAssignedGrid()
        End Sub

    End Class

End Namespace
