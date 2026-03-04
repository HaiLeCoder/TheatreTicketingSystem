Hệ thống Bán Vé Nhà Hát (VB.NET + PostgreSQL)

Tổng quan:
Bạn sẽ xây dựng một ứng dụng WinForms nhỏ bằng VB.NET, mô phỏng hệ thống bán vé cho nhà hát.
Yêu cầu phát triển 3 chức năng chính:
Quản lý suất diễn
Đặt vé cho khách hàng
Gán ghế cho từng vé trong sơ đồ ghế (có kiểm tra trùng lặp)
Bài test sẽ đánh giá:
Kỹ năng lập trình ứng dụng desktop bằng VB.NET
Thiết kế và thao tác cơ sở dữ liệu PostgreSQL
Tư duy logic nghiệp vụ, xử lý dữ liệu liên kết
Khả năng kiểm soát UI động, kiểm tra lỗi và tương tác người dùng
Thời gian thực hiện đề xuất: khoảng 6 giờ làm việc, hoặc hoàn thành trong 1–2 ngày làm việc bán thời gian
Chi tiết yêu cầu:
Chức năng 1 – Quản lý suất diễn (Cơ bản)
Tạo form frmPerformanceMaster để quản lý danh sách suất diễn, bao gồm các thông tin cơ bản:
Tên vở diễn
Thời gian bắt đầu
Thời lượng (số phút)
…vv
Cho phép: Thêm mới / Sửa / Xóa suất diễn
Sử dụng DataGridView để hiển thị danh sách
Search suất diễn theo tên, khoảng thời gian
Chức năng 2 – Đặt vé (Cơ bản)
Tạo form frmBooking cho phép người dùng:
Chọn suất diễn (ComboBox)
Nhập tên khách hàng
Chọn loại ghế (Ghế thường / Ghế VIP / Ghế đôi)
Nhập số lượng vé
Khi nhấn [Đặt vé], thông tin sẽ được lưu vào bảng bookings.
Hiển thị tổng tiền dựa trên loại ghế.
Chức năng 3 – Gán ghế theo sơ đồ & Kiểm tra trùng (Nâng cao)
Tạo form frmSeatAssignment với các yêu cầu sau:
Mục tiêu:
Gán vị trí cụ thể (ghế) cho từng vé trong một booking
Tránh ghế bị trùng giữa các booking khác nhau
Chi tiết:
Người dùng chọn một booking (ví dụ bằng Booking ID)
Hiển thị sơ đồ ghế: 10 hàng (A–J) × 10 cột (1–10) bằng các Button hoặc Label
Trạng thái ghế:
🟥 Đỏ: ghế đã được gán cho booking khác → không thể chọn
🟦 Xanh: ghế đang được chọn cho booking hiện tại
⬜ Trắng: ghế trống, có thể chọn
Người dùng được chọn tối đa N ghế, trong đó N là số lượng vé đã đặt
Nếu chọn quá N ghế → hiện cảnh báo
Có thể bỏ chọn ghế đã chọn nhầm
Nhấn [Lưu] để ghi các ghế vào bảng seat_assignments
Ràng buộc logic:
Mỗi booking phải được gán đúng số ghế
Không được trùng ghế với booking khác cùng suất diễn
Thiết kế database:
Hãy thiết kế database với các bảng như performances, bookings, seat_assignments …vv để có thể đáp ứng được yêu cầu đề ra.
Yêu cầu giao diện (UI):
Sử dụng DataGridView, ComboBox, TextBox, Button, Panel hoặc các control chuẩn trong WinForms
Có thông báo rõ ràng nếu:
Nhập thiếu thông tin
Chọn ghế vượt quá số lượng vé
Ghế bị trùng với người khác
Có popup xác nhận khi xóa hoặc lưu
Bonus nếu muốn nổi bật:
Báo cáo: số lượng ghế từng loại theo từng suất diễn
Tìm kiếm suất diễn theo tên trong form đặt vé
Hiển thị danh sách ghế đã gán cho một booking

Yêu cầu 
File .sql tạo các bảng
File README.md mô tả:
Cách chạy chương trình
Cách kết nối PostgreSQL
Các giả định hoặc giới hạn (nếu có)

