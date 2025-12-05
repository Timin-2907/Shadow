namespace ECommerceMVC.ViewModels
{
    public class OrderHistory
    {
        public int MaHD { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public decimal TongTien { get; set; }
        public string VnPayTransactionId { get; set; }
        public List<OrderDetailViewModel> ChiTiet { get; set; }
    }

    public class OrderDetailViewModel
    {
        public string TenHH { get; set; }
        public string Hinh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}
