Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Services
Imports TheatreTicketingSystem.Utils

Namespace Forms

    ''' <summary>
    ''' frmBooking – Chức năng 2: Đặt vé / quản lý booking
    ''' RC-016 FIX: Inherits BaseForm – removed duplicate helper methods.
    ''' </summary>
    Public Class frmBooking
        Inherits BaseForm   ' RC-016

        Private ReadOnly _bookingService     As BookingService     = BookingService.Instance
        Private ReadOnly _performanceService As PerformanceService = PerformanceService.Instance

        Private _performances As List(Of Performance)
        Private _seatTypes    As List(Of SeatType)

        Private pnlTop      As Panel
        Private pnlLeft     As Panel
        Private pnlRight    As Panel

        Private txtSearchPerf   As TextBox
        Private WithEvents btnSearchPerf As Button
        Private cboPerformance  As ComboBox

        Private txtCustomerName  As TextBox
        Private txtCustomerPhone As TextBox
        Private cboSeatType      As ComboBox
        Private nudTicketCount   As NumericUpDown
        Private lblTotalAmount   As Label
        Private txtNotes         As TextBox

        Private WithEvents btnBook         As Button
        Private WithEvents btnCancelForm   As Button
        Private WithEvents btnConfirm      As Button
        Private WithEvents btnCancelBooking As Button
        Private WithEvents btnRefresh      As Button

        Private dgvBookings As DataGridView

        Public Sub New()
            InitializeComponent()
            LoadPerformances()
            LoadSeatTypes()
            LoadBookingGrid()
        End Sub

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.BackColor = ClrBackground
            Me.ForeColor = ClrTextPrimary
            Me.Font      = FontNormal
            Me.Padding   = New Padding(15)
            BuildTopBar()
            BuildLeftPanel()
            BuildRightPanel()
            Me.ResumeLayout(False)
        End Sub

        Private Sub BuildTopBar()
            pnlTop            = New Panel()
            pnlTop.Dock       = DockStyle.Top
            pnlTop.Height     = 55
            pnlTop.BackColor  = ClrBackground

            Dim lbl           = CreateLabel("🎟️  ĐẶT VÉ", 0, 12, bold:=True, size:=14)
            lbl.ForeColor     = Color.FromArgb(100, 210, 170)
            pnlTop.Controls.Add(lbl)
            Me.Controls.Add(pnlTop)
        End Sub

        Private Sub BuildLeftPanel()
            pnlLeft           = New Panel()
            pnlLeft.Location  = New Point(15, 70)
            pnlLeft.Size      = New Size(480, 680)
            pnlLeft.Anchor    = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
            pnlLeft.BackColor = ClrPanelDark
            pnlLeft.Padding   = New Padding(18)

            Dim lblSec        = CreateLabel("Thông tin đặt vé", 18, 15, bold:=True, size:=11)
            lblSec.ForeColor  = Color.FromArgb(100, 210, 170)
            pnlLeft.Controls.Add(lblSec)

            Dim y = 50

            ' -- Suất diễn
            pnlLeft.Controls.Add(CreateLabel("Suất diễn *", 18, y)) : y += 22
            txtSearchPerf                 = New TextBox()
            txtSearchPerf.Location        = New Point(18, y)
            txtSearchPerf.Size            = New Size(280, 28)
            txtSearchPerf.BackColor       = ClrInput
            txtSearchPerf.ForeColor       = ClrTextPrimary
            txtSearchPerf.PlaceholderText = "Tìm suất diễn..."
            pnlLeft.Controls.Add(txtSearchPerf)

            btnSearchPerf           = New Button()
            btnSearchPerf.Text      = "🔍"
            btnSearchPerf.Location  = New Point(305, y)
            btnSearchPerf.Size      = New Size(45, 28)
            btnSearchPerf.FlatStyle = FlatStyle.Flat
            btnSearchPerf.BackColor = Color.FromArgb(60, 80, 140)
            btnSearchPerf.ForeColor = Color.White
            btnSearchPerf.FlatAppearance.BorderSize = 0
            pnlLeft.Controls.Add(btnSearchPerf)
            y += 35

            cboPerformance               = New ComboBox()
            cboPerformance.Location      = New Point(18, y)
            cboPerformance.Size          = New Size(440, 30)
            cboPerformance.DropDownStyle = ComboBoxStyle.DropDownList
            cboPerformance.BackColor     = ClrInput
            cboPerformance.ForeColor     = ClrTextPrimary
            pnlLeft.Controls.Add(cboPerformance)
            AddHandler cboPerformance.SelectedIndexChanged, AddressOf RecalcTotal
            y += 45

            ' -- Khách hàng
            pnlLeft.Controls.Add(CreateLabel("Tên khách hàng *", 18, y)) : y += 22
            txtCustomerName = CreateTextBox(18, y, 440) : pnlLeft.Controls.Add(txtCustomerName) : y += 40

            pnlLeft.Controls.Add(CreateLabel("Số điện thoại", 18, y)) : y += 22
            txtCustomerPhone = CreateTextBox(18, y, 220) : pnlLeft.Controls.Add(txtCustomerPhone) : y += 40

            ' -- Loại ghế
            pnlLeft.Controls.Add(CreateLabel("Loại ghế *", 18, y)) : y += 22
            cboSeatType               = New ComboBox()
            cboSeatType.Location      = New Point(18, y)
            cboSeatType.Size          = New Size(250, 30)
            cboSeatType.DropDownStyle = ComboBoxStyle.DropDownList
            cboSeatType.BackColor     = ClrInput
            cboSeatType.ForeColor     = ClrTextPrimary
            pnlLeft.Controls.Add(cboSeatType)
            AddHandler cboSeatType.SelectedIndexChanged, AddressOf RecalcTotal
            y += 45

            ' -- Số lượng
            pnlLeft.Controls.Add(CreateLabel("Số lượng vé *", 18, y)) : y += 22
            nudTicketCount          = New NumericUpDown()
            nudTicketCount.Location = New Point(18, y)
            nudTicketCount.Size     = New Size(100, 28)
            nudTicketCount.Minimum  = 1 : nudTicketCount.Maximum = 100 : nudTicketCount.Value = 1
            StyleNumeric(nudTicketCount)
            pnlLeft.Controls.Add(nudTicketCount)
            AddHandler nudTicketCount.ValueChanged, AddressOf RecalcTotal
            y += 40

            ' -- Total
            Dim lblTTxt    = CreateLabel("Tổng tiền:", 18, y, bold:=True)
            lblTotalAmount = CreateLabel("0 VNĐ", 130, y, bold:=True, size:=12)
            lblTotalAmount.ForeColor = Color.FromArgb(255, 200, 50)
            pnlLeft.Controls.AddRange({lblTTxt, lblTotalAmount})
            y += 35

            ' -- Ghi chú
            pnlLeft.Controls.Add(CreateLabel("Ghi chú", 18, y)) : y += 22
            txtNotes          = New TextBox()
            txtNotes.Location = New Point(18, y)
            txtNotes.Size     = New Size(440, 55)
            txtNotes.Multiline = True
            txtNotes.BackColor = ClrInput
            txtNotes.ForeColor = ClrTextPrimary
            pnlLeft.Controls.Add(txtNotes)
            y += 65

            ' -- Buttons
            btnBook                       = New Button()
            btnBook.Text                  = "🎟  Đặt vé"
            btnBook.Location              = New Point(18, y)
            btnBook.Size                  = New Size(150, 42)
            btnBook.BackColor             = Color.FromArgb(30, 130, 80)
            btnBook.ForeColor             = Color.White
            btnBook.FlatStyle             = FlatStyle.Flat
            btnBook.FlatAppearance.BorderSize = 0
            btnBook.Font                  = New Font("Segoe UI", 11, FontStyle.Bold)
            btnBook.Cursor                = Cursors.Hand

            btnCancelForm                 = New Button()
            btnCancelForm.Text            = "✕ Hủy"
            btnCancelForm.Location        = New Point(180, y)
            btnCancelForm.Size            = New Size(100, 42)
            btnCancelForm.BackColor       = Color.FromArgb(80, 80, 100)
            btnCancelForm.ForeColor       = Color.White
            btnCancelForm.FlatStyle       = FlatStyle.Flat
            btnCancelForm.FlatAppearance.BorderSize = 0
            btnCancelForm.Cursor          = Cursors.Hand

            pnlLeft.Controls.AddRange({btnBook, btnCancelForm})
            Me.Controls.Add(pnlLeft)
        End Sub

        Private Sub BuildRightPanel()
            pnlRight           = New Panel()
            pnlRight.Location  = New Point(510, 70)
            pnlRight.Size      = New Size(660, 680)
            pnlRight.Anchor    = AnchorStyles.Top Or AnchorStyles.Bottom Or
                                 AnchorStyles.Left Or AnchorStyles.Right
            pnlRight.BackColor = ClrPanelDark

            Dim lbl            = CreateLabel("Danh sách booking", 12, 12, bold:=True)
            lbl.ForeColor      = Color.FromArgb(100, 210, 170)
            pnlRight.Controls.Add(lbl)

            dgvBookings          = New DataGridView()
            dgvBookings.Location = New Point(12, 40)
            dgvBookings.Size     = New Size(636, 560)
            dgvBookings.Anchor   = AnchorStyles.Top Or AnchorStyles.Bottom Or
                                   AnchorStyles.Left Or AnchorStyles.Right
            StyleGrid(dgvBookings)  ' RC-016: BaseForm helper

            Dim bookingCols As (Name As String, Hdr As String, W As Integer)() = {
                ("Id",              "ID",          35),
                ("PerformanceName", "Suất diễn",  120),
                ("CustomerName",    "Khách hàng",  100),
                ("SeatTypeName",    "Loại ghế",    80),
                ("TicketCount",     "Số vé",        50),
                ("TotalAmount",     "Tổng tiền",    90),
                ("StatusDisplay",   "Trạng thái",   90),
                ("SeatsAssigned",   "Ghế gán",      60)
            }
            For Each col In bookingCols
                Dim c As New DataGridViewTextBoxColumn()
                c.Name             = col.Name
                c.DataPropertyName = col.Name
                c.HeaderText       = col.Hdr
                c.FillWeight       = col.W
                dgvBookings.Columns.Add(c)
            Next
            pnlRight.Controls.Add(dgvBookings)

            btnConfirm               = New Button()
            btnConfirm.Text          = "✔ Xác nhận booking"
            btnConfirm.Location      = New Point(12, 610)
            btnConfirm.Size          = New Size(170, 38)
            btnConfirm.BackColor     = Color.FromArgb(30, 100, 50)
            btnConfirm.ForeColor     = Color.White
            btnConfirm.FlatStyle     = FlatStyle.Flat
            btnConfirm.FlatAppearance.BorderSize = 0
            btnConfirm.Cursor        = Cursors.Hand

            btnCancelBooking         = New Button()
            btnCancelBooking.Text    = "✖ Huỷ booking"
            btnCancelBooking.Location = New Point(192, 610)
            btnCancelBooking.Size    = New Size(140, 38)
            btnCancelBooking.BackColor = Color.FromArgb(140, 40, 40)
            btnCancelBooking.ForeColor = Color.White
            btnCancelBooking.FlatStyle = FlatStyle.Flat
            btnCancelBooking.FlatAppearance.BorderSize = 0
            btnCancelBooking.Cursor  = Cursors.Hand

            btnRefresh               = New Button()
            btnRefresh.Text          = "🔄 Làm mới"
            btnRefresh.Location      = New Point(344, 610)
            btnRefresh.Size          = New Size(110, 38)
            btnRefresh.BackColor     = Color.FromArgb(50, 80, 120)
            btnRefresh.ForeColor     = Color.White
            btnRefresh.FlatStyle     = FlatStyle.Flat
            btnRefresh.FlatAppearance.BorderSize = 0
            btnRefresh.Cursor        = Cursors.Hand

            pnlRight.Controls.AddRange({btnConfirm, btnCancelBooking, btnRefresh})
            Me.Controls.Add(pnlRight)
        End Sub

        ' ── Data ──────────────────────────────────────────────────────────────

        Private Sub LoadPerformances(Optional filter As String = "")
            Try
                _performances = If(String.IsNullOrWhiteSpace(filter),
                                   _performanceService.GetAll(),
                                   _performanceService.Search(filter, Nothing, Nothing))
                cboPerformance.DataSource    = Nothing
                cboPerformance.Items.Clear()
                For Each p In _performances : cboPerformance.Items.Add(p) : Next
                cboPerformance.DisplayMember = "Name"
                If cboPerformance.Items.Count > 0 Then cboPerformance.SelectedIndex = 0
            Catch ex As Exception
                ShowError("Lỗi tải suất diễn", ex)
            End Try
        End Sub

        Private Sub LoadSeatTypes()
            Try
                _seatTypes               = _bookingService.GetSeatTypes()
                cboSeatType.DataSource   = _seatTypes
                cboSeatType.DisplayMember = "Name"
                cboSeatType.ValueMember  = "Id"
            Catch ex As Exception
                ShowError("Lỗi tải loại ghế", ex)
            End Try
        End Sub

        Private Sub LoadBookingGrid()
            Try
                Dim list = _bookingService.GetAll()
                Dim dt   = New DataTable()
                dt.Columns.AddRange({
                    New DataColumn("Id",              GetType(Integer)),
                    New DataColumn("PerformanceName", GetType(String)),
                    New DataColumn("CustomerName",    GetType(String)),
                    New DataColumn("SeatTypeName",    GetType(String)),
                    New DataColumn("TicketCount",     GetType(Integer)),
                    New DataColumn("TotalAmount",     GetType(String)),
                    New DataColumn("StatusDisplay",   GetType(String)),
                    New DataColumn("SeatsAssigned",   GetType(String))
                })
                For Each b In list
                    dt.Rows.Add(b.Id, b.PerformanceName, b.CustomerName,
                                b.SeatTypeName, b.TicketCount,
                                $"{b.TotalAmount:N0} đ",
                                b.StatusDisplay,
                                $"{b.SeatsAssigned}/{b.TicketCount}")
                Next
                dgvBookings.DataSource = dt
            Catch ex As Exception
                ShowError("Lỗi tải danh sách booking", ex)
            End Try
        End Sub

        ' ── Events ────────────────────────────────────────────────────────────

        Private Sub btnSearchPerf_Click(s As Object, e As EventArgs) Handles btnSearchPerf.Click
            LoadPerformances(txtSearchPerf.Text.Trim())
        End Sub

        Private Sub RecalcTotal(sender As Object, e As EventArgs)
            If cboSeatType.SelectedItem Is Nothing Then Return
            Dim st    = CType(cboSeatType.SelectedItem, SeatType)
            Dim total = st.Price * nudTicketCount.Value
            lblTotalAmount.Text = $"{total:N0} VNĐ"
        End Sub

        Private Sub btnBook_Click(s As Object, e As EventArgs) Handles btnBook.Click
            Try
                If cboPerformance.SelectedItem Is Nothing Then
                    ShowWarning("Vui lòng chọn suất diễn.")
                    Return
                End If
                Dim perf = CType(cboPerformance.SelectedItem, Performance)
                Dim st   = CType(cboSeatType.SelectedItem, SeatType)
                Dim b As New Booking With {
                    .PerformanceId = perf.Id,
                    .SeatTypeId    = st.Id,
                    .CustomerName  = txtCustomerName.Text.Trim(),
                    .CustomerPhone = txtCustomerPhone.Text.Trim(),
                    .TicketCount   = CInt(nudTicketCount.Value),
                    .Notes         = txtNotes.Text.Trim()
                }
                Dim created = _bookingService.Create(b)
                ShowSuccess($"Đặt vé thành công! Booking ID: #{created.Id}" &
                            Environment.NewLine &
                            $"Tổng tiền: {created.TotalAmount:N0} VNĐ" &
                            Environment.NewLine &
                            "Vào 'Gán ghế' để chọn vị trí ghế.")
                ClearForm()
                LoadBookingGrid()
            Catch ex As ArgumentException
                ShowWarning(ex.Message)
            Catch ex As Exception
                ShowError("Lỗi đặt vé", ex)
            End Try
        End Sub

        Private Sub btnCancelForm_Click(s As Object, e As EventArgs) Handles btnCancelForm.Click
            ClearForm()
        End Sub

        Private Sub btnConfirm_Click(s As Object, e As EventArgs) Handles btnConfirm.Click
            Dim id = GetSelectedBookingId()
            If id <= 0 Then Return
            Try
                _bookingService.Confirm(id)
                ShowSuccess($"Booking #{id} đã xác nhận!")
                LoadBookingGrid()
            Catch ex As InvalidOperationException
                ShowWarning(ex.Message)
            Catch ex As Exception
                ShowError("Lỗi xác nhận booking", ex)
            End Try
        End Sub

        Private Sub btnCancelBooking_Click(s As Object, e As EventArgs) Handles btnCancelBooking.Click
            Dim id = GetSelectedBookingId()
            If id <= 0 Then Return
            If Confirm($"Huỷ booking #{id}?", "Xác nhận huỷ") Then
                Try
                    _bookingService.Cancel(id)
                    ShowSuccess($"Booking #{id} đã huỷ.")
                    LoadBookingGrid()
                Catch ex As Exception
                    ShowError("Lỗi huỷ booking", ex)
                End Try
            End If
        End Sub

        Private Sub btnRefresh_Click(s As Object, e As EventArgs) Handles btnRefresh.Click
            LoadBookingGrid()
        End Sub

        ' ── Helpers ───────────────────────────────────────────────────────────

        Private Function GetSelectedBookingId() As Integer
            If dgvBookings.SelectedRows.Count = 0 Then
                ShowWarning("Vui lòng chọn một booking trong danh sách.")
                Return 0
            End If
            Return CInt(dgvBookings.SelectedRows(0).Cells("Id").Value)
        End Function

        Private Sub ClearForm()
            txtCustomerName.Clear()
            txtCustomerPhone.Clear()
            txtNotes.Clear()
            nudTicketCount.Value = 1
            If cboSeatType.Items.Count > 0 Then cboSeatType.SelectedIndex = 0
            RecalcTotal(Nothing, EventArgs.Empty)
        End Sub

    End Class

End Namespace


