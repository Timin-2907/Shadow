BEGIN TRAN;

-- 1) Xem hiện trạng hiện tại (kiểm tra trước khi thay đổi)
SELECT MaKh, MatKhau, RandomKey, VaiTro
FROM KhachHang
WHERE MaKh = 'admin';

-- 2) Nếu RandomKey đang NULL thì tạo 1 giá trị ngẫu nhiên ngắn
UPDATE KhachHang
SET RandomKey = LEFT(REPLACE(NEWID(), '-', ''), 5)
WHERE MaKh = 'admin' AND RandomKey IS NULL;

-- 3) Cập nhật MatKhau thành MD5(password + RandomKey) (lowercase hex) và đảm bảo VaiTro = 1 (Admin)
-- Thay 'admin123' bằng mật khẩu bạn muốn dùng
UPDATE KhachHang
SET MatKhau = LOWER(CONVERT(VARCHAR(32), HASHBYTES('MD5', CONCAT('admin123', RandomKey)), 2)),
    VaiTro = 1
WHERE MaKh = 'admin';

-- 4) Kiểm tra lại
SELECT MaKh, MatKhau, RandomKey, VaiTro
FROM KhachHang
WHERE MaKh = 'admin';

COMMIT;