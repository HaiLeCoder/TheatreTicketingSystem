Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports TheatreTicketingSystem.Models
Imports TheatreTicketingSystem.Services

Namespace Forms

    ''' <summary>
    ''' frmPerformanceMaster – Chức năng 1: Quản lý suất diễn
    ''' SelectionChanged hiện gọi _service.GetById() thay vì
    ''' _service.GetAll().FirstOrDefault(), loại bỏ truy vấn N+1.
    ''' Kế thừa từ BaseForm – loại bỏ các phương thức bổ trợ trùng lặp.
    ''' </summary>
    Public Class frmPerformanceMaster
        Inherits BaseForm   ' Kế thừa các helper chung

        ' ── Services ──────────────────────────────────────────────────────────
        Private ReadOnly _service As PerformanceService = PerformanceService.Instance

        ' ── State ─────────────────────────────────────────────────────────────
        Private _selectedPerformance As Performance = Nothing

        ' ── Controls ──────────────────────────────────────────────────────────
        Private pnlSearch       As Panel
        Private pnlGrid         As Panel
        Private pnlForm         As Panel
        Private pnlButtons      As Panel

        Private txtSearch       As TextBox
        Private dtpFrom         As DateTimePicker
        Private dtpTo           As DateTimePicker
        Private WithEvents chkFilterDate   As CheckBox
        Private WithEvents btnSearch As Button

        Private dgvList         As DataGridView

        Private txtName         As TextBox
        Private txtLocation     As TextBox
        Private txtDescription  As RichTextBox
        Private dtpStartTime    As DateTimePicker
        Private nudDuration     As NumericUpDown
        Private nudTotalSeats   As NumericUpDown

        Private WithEvents btnNew    As Button
        Private WithEvents btnSave   As Button
        Private WithEvents btnDelete As Button
        Private WithEvents btnCancelEdit As Button

        ' ── Constructor ───────────────────────────────────────────────────────

        Public Sub New()
            InitializeComponent()
            LoadGrid()
            SetFormMode(editing:=False)
        End Sub

        ' ── UI Construction ───────────────────────────────────────────────────

        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.BackColor = ClrBackground
            Me.ForeColor = ClrTextPrimary
            Me.Font      = FontNormal
            Me.Padding   = New Padding(15)

            BuildSearchPanel()
            BuildGridPanel()
            BuildFormPanel()
            BuildButtonBar()

            Me.ResumeLayout(False)
        End Sub

        Private Sub BuildSearchPanel()
            pnlSearch = CreatePanel(DockStyle.Top, 90)

            Dim lblTitle       = CreateLabel("🎪  QUẢN LÝ SUẤT DIỄN", 0, 8, bold:=True, size:=13)
            lblTitle.ForeColor = Color.FromArgb(180, 130, 255)

            Dim lblTF  = CreateLabel("Tên vở:", 0, 50)
            txtSearch  = CreateTextBox(70, 47, 160)
            txtSearch.PlaceholderText = "Tìm theo tên..."

            chkFilterDate          = New CheckBox()
            chkFilterDate.Text     = "Lọc theo ngày:"
            chkFilterDate.Location = New Point(250, 50)
            chkFilterDate.ForeColor = Color.FromArgb(200, 200, 220)
            chkFilterDate.AutoSize = True

            dtpFrom  = CreateDtp(380, 47, 120)
            dtpFrom.Format = DateTimePickerFormat.Custom
            dtpFrom.CustomFormat = "yyyy/MM/dd"
            dtpFrom.Value        = DateTime.Today
            dtpFrom.Enabled      = False

            Dim lblD = CreateLabel("→", 510, 50)
            dtpTo    = CreateDtp(540, 47, 120)
            dtpTo.Format = DateTimePickerFormat.Custom
            dtpTo.CustomFormat = "yyyy/MM/dd"
            dtpTo.Value        = DateTime.Today
            dtpTo.Enabled      = False

            btnSearch = CreateButton("🔍 Tìm", 680, 45, 100, 35, Color.FromArgb(60, 100, 160))

            pnlSearch.Controls.AddRange({lblTitle, lblTF, txtSearch,
                                          chkFilterDate, dtpFrom, lblD, dtpTo, btnSearch})
            Me.Controls.Add(pnlSearch)
        End Sub

        Private Sub BuildGridPanel()
            pnlGrid          = CreatePanel(DockStyle.None, 0)
            pnlGrid.Location = New Point(15, 110)
            pnlGrid.Size     = New Size(Me.ClientSize.Width - 420 - 45, Me.ClientSize.Height - 110 - 15)
            pnlGrid.Anchor   = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right

            dgvList = New DataGridView()
            dgvList.Dock = DockStyle.Fill
            StyleGrid(dgvList)   ' Sử dụng helper từ BaseForm

            Dim cols As (Name As String, Header As String, FillW As Integer)() = {
                ("Id",              "ID",          40),
                ("Name",            "Tên vở diễn",140),
                ("StartTime",       "Thời gian",   90),
                ("DurationMinutes", "Thời lượng",  70),
                ("Location",        "Địa điểm",   110),
                ("TotalSeats",      "Tổng ghế",    55)
            }
            For Each col In cols
                Dim c As New DataGridViewTextBoxColumn()
                c.Name             = col.Name
                c.DataPropertyName = col.Name
                c.HeaderText       = col.Header
                c.FillWeight       = col.FillW
                dgvList.Columns.Add(c)
            Next

            AddHandler dgvList.SelectionChanged, AddressOf dgvList_SelectionChanged
            pnlGrid.Controls.Add(dgvList)
            Me.Controls.Add(pnlGrid)
        End Sub

        Private Sub BuildFormPanel()
            pnlForm          = New Panel()
            pnlForm.Location = New Point(Me.ClientSize.Width - 420 - 15, 110)
            pnlForm.Size     = New Size(420, Me.ClientSize.Height - 110 - 75) ' Leave space for bottom buttons
            pnlForm.Anchor   = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Right
            pnlForm.BackColor = ClrPanelDark
            pnlForm.Padding  = New Padding(15)

            Dim lblForm       = CreateLabel("Chi tiết suất diễn", 15, 15, bold:=True, size:=11)
            lblForm.ForeColor = Color.FromArgb(180, 130, 255)

            Dim y = 50
            pnlForm.Controls.Add(CreateLabel("Tên vở diễn *", 15, y))
            y += 22 : txtName = CreateTextBox(15, y, 390) : y += 40

            pnlForm.Controls.Add(CreateLabel("Thời gian bắt đầu *", 15, y))
            y += 22
            dtpStartTime              = CreateDtp(15, y, 250)
            dtpStartTime.Format       = DateTimePickerFormat.Custom
            dtpStartTime.CustomFormat = "dd/MM/yyyy HH:mm"
            dtpStartTime.Value        = DateTime.Now.AddDays(1)
            y += 40

            pnlForm.Controls.Add(CreateLabel("Thời lượng (phút) *", 15, y))
            y += 22
            nudDuration          = New NumericUpDown()
            nudDuration.Location = New Point(15, y)
            nudDuration.Size     = New Size(120, 28)
            nudDuration.Minimum  = 10 : nudDuration.Maximum = 720 : nudDuration.Value = 90
            StyleNumeric(nudDuration)
            y += 40

            ' RC-010: max seats now capped at 100 in UI too
            pnlForm.Controls.Add(CreateLabel("Tổng số ghế * (tối đa 100)", 15, y))
            y += 22
            nudTotalSeats          = New NumericUpDown()
            nudTotalSeats.Location = New Point(15, y)
            nudTotalSeats.Size     = New Size(120, 28)
            nudTotalSeats.Minimum  = 1 : nudTotalSeats.Maximum = 100 : nudTotalSeats.Value = 100
            StyleNumeric(nudTotalSeats)
            y += 40

            pnlForm.Controls.Add(CreateLabel("Địa điểm", 15, y))
            y += 22 : txtLocation = CreateTextBox(15, y, 390) : y += 40

            pnlForm.Controls.Add(CreateLabel("Mô tả", 15, y))
            y += 22
            txtDescription           = New RichTextBox()
            txtDescription.Location  = New Point(15, y)
            txtDescription.Size      = New Size(390, 80)
            txtDescription.BackColor = ClrInput
            txtDescription.ForeColor = ClrTextPrimary
            txtDescription.BorderStyle = BorderStyle.None

            pnlForm.Controls.AddRange({lblForm, txtName, dtpStartTime, nudDuration,
                                        nudTotalSeats, txtLocation, txtDescription})
            Me.Controls.Add(pnlForm)
        End Sub

        Private Sub BuildButtonBar()
            pnlButtons          = New Panel()
            pnlButtons.Location = New Point(Me.ClientSize.Width - 420 - 15, Me.ClientSize.Height - 65) ' Aligning properly
            pnlButtons.Size     = New Size(420, 55)
            pnlButtons.Anchor   = AnchorStyles.Bottom Or AnchorStyles.Right
            pnlButtons.BackColor = ClrBackground

            btnNew        = CreateButton("➕ Thêm vở",   10, 10, 95, 38, Color.FromArgb(40, 100, 60))
            btnSave       = CreateButton("💾 Lưu",  110, 10, 90, 38, Color.FromArgb(40, 80, 160))
            btnDelete     = CreateButton("🗑 Xóa",  210, 10, 90, 38, Color.FromArgb(140, 40, 40))
            btnCancelEdit = CreateButton("✕ Hủy",  310, 10, 90, 38, Color.FromArgb(80, 80, 100))

            pnlButtons.Controls.AddRange({btnNew, btnSave, btnDelete, btnCancelEdit})
            Me.Controls.Add(pnlButtons)
        End Sub

        ' ── Data ──────────────────────────────────────────────────────────────

        Private Sub LoadGrid(Optional nameFilter As String = "",
                              Optional fromDate As DateTime? = Nothing,
                              Optional toDate As DateTime? = Nothing)
            Try
                Dim list = If(String.IsNullOrWhiteSpace(nameFilter) AndAlso Not fromDate.HasValue,
                              _service.GetAll(),
                              _service.Search(nameFilter, fromDate, toDate))

                Dim dt As New DataTable()
                dt.Columns.AddRange({
                    New DataColumn("Id",              GetType(Integer)),
                    New DataColumn("Name",            GetType(String)),
                    New DataColumn("StartTime",       GetType(String)),
                    New DataColumn("DurationMinutes", GetType(String)),
                    New DataColumn("Location",        GetType(String)),
                    New DataColumn("TotalSeats",      GetType(Integer))
                })
                For Each p In list
                    dt.Rows.Add(p.Id, p.Name,
                                p.StartTime.ToString("dd/MM/yyyy HH:mm"),
                                $"{p.DurationMinutes} phút",
                                p.Location, p.TotalSeats)
                Next
                dgvList.DataSource = dt
            Catch ex As Exception
                ShowError("Lỗi tải danh sách suất diễn", ex)
            End Try
        End Sub

        Private Sub BindFormTo(p As Performance)
            txtName.Text           = p.Name
            txtLocation.Text       = p.Location
            txtDescription.Text    = p.Description
            dtpStartTime.Value     = If(p.StartTime = DateTime.MinValue,
                                        DateTime.Now.AddDays(1), p.StartTime)
            nudDuration.Value      = Math.Max(10, p.DurationMinutes)
            nudTotalSeats.Value    = Math.Max(1, Math.Min(100, p.TotalSeats))  ' RC-010: clamp to 100
        End Sub

        Private Function ReadFormPerformance() As Performance
            Return New Performance With {
                .Id              = If(_selectedPerformance IsNot Nothing, _selectedPerformance.Id, 0),
                .Name            = txtName.Text.Trim(),
                .Location        = txtLocation.Text.Trim(),
                .Description     = txtDescription.Text.Trim(),
                .StartTime       = dtpStartTime.Value,
                .DurationMinutes = CInt(nudDuration.Value),
                .TotalSeats      = CInt(nudTotalSeats.Value)
            }
        End Function

        Private Sub SetFormMode(editing As Boolean)
            Dim e = editing
            txtName.Enabled        = e
            txtLocation.Enabled    = e
            txtDescription.Enabled = e
            dtpStartTime.Enabled   = e
            nudDuration.Enabled    = e
            nudTotalSeats.Enabled  = e
            btnSave.Enabled        = e
            btnCancelEdit.Enabled  = e
            ' btnDelete is enabled when we have a selection but NOT when adding a brand new performance
            btnDelete.Enabled      = (_selectedPerformance IsNot Nothing AndAlso _selectedPerformance.Id > 0)
        End Sub

        ' ── Events ────────────────────────────────────────────────────────────

        Private Sub chkFilterDate_CheckedChanged(sender As Object, e As EventArgs) Handles chkFilterDate.CheckedChanged
            dtpFrom.Enabled = chkFilterDate.Checked
            dtpTo.Enabled   = chkFilterDate.Checked
        End Sub

        Private Sub btnSearch_Click(sender As Object, e As EventArgs) Handles btnSearch.Click
            Try
                Dim fd As DateTime? = Nothing
                Dim td As DateTime? = Nothing
                If chkFilterDate.Checked Then
                    fd = dtpFrom.Value.Date
                    td = dtpTo.Value.Date.AddDays(1).AddSeconds(-1)
                End If
                LoadGrid(txtSearch.Text.Trim(), fd, td)
            Catch ex As Exception
                ShowError("Lỗi tìm kiếm", ex)
            End Try
        End Sub

        Private Sub dgvList_SelectionChanged(sender As Object, e As EventArgs)
            If dgvList.SelectedRows.Count = 0 Then Return
            Dim selectedId = CInt(dgvList.SelectedRows(0).Cells("Id").Value)
            Try
                ' RC-006 FIX: Use GetById (1 query) instead of GetAll().FirstOrDefault (N+1)
                ' Kích hoạt chỉnh sửa ngay lập tức khi chọn
                _selectedPerformance = _service.GetById(selectedId)
                If _selectedPerformance IsNot Nothing Then
                    BindFormTo(_selectedPerformance)
                    SetFormMode(editing:=True)
                End If
            Catch ex As Exception
                ShowError("Lỗi tải chi tiết suất diễn", ex)
            End Try
        End Sub

        Private Sub btnNew_Click(sender As Object, e As EventArgs) Handles btnNew.Click
            _selectedPerformance = New Performance()
            BindFormTo(_selectedPerformance)
            SetFormMode(editing:=True)
            txtName.Focus()
        End Sub

        Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
            Try
                Dim p = ReadFormPerformance()
                If p.Id = 0 Then
                    _service.Create(p)
                    ShowSuccess("Tạo suất diễn thành công!")
                Else
                    _service.Update(p)
                    ShowSuccess("Cập nhật suất diễn thành công!")
                End If
                LoadGrid()
                SetFormMode(editing:=False)
            Catch ex As ArgumentException
                ShowWarning(ex.Message)
            Catch ex As Exception
                ShowError("Lỗi lưu suất diễn", ex)
            End Try
        End Sub

        Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
            If _selectedPerformance Is Nothing Then Return
            If Confirm($"Xóa suất diễn ""{_selectedPerformance.Name}""?", "Xác nhận xóa") Then
                Try
                    _service.Delete(_selectedPerformance.Id)
                    ShowSuccess("Đã xóa suất diễn.")
                    _selectedPerformance = Nothing
                    LoadGrid()
                    SetFormMode(editing:=False)
                Catch ex As InvalidOperationException
                    ShowWarning(ex.Message)
                Catch ex As Exception
                    ShowError("Lỗi xóa suất diễn", ex)
                End Try
            End If
        End Sub

        Private Sub btnCancelEdit_Click(sender As Object, e As EventArgs) Handles btnCancelEdit.Click
            If _selectedPerformance IsNot Nothing Then BindFormTo(_selectedPerformance)
            SetFormMode(editing:=False)
        End Sub

    End Class

End Namespace
