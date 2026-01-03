using System;
using Microsoft.EntityFrameworkCore;

namespace ECommerceMVC.Models
{
    public partial class ShoeContext : DbContext
    {
        public ShoeContext()
        {
        }

        public ShoeContext(DbContextOptions<ShoeContext> options)
            : base(options)
        {
        }

        public virtual DbSet<KhachHang> KhachHangs { get; set; }
        public virtual DbSet<Role> Roles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=Shoe;Integrated Security=True;Trust Server Certificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<KhachHang>(entity =>
            {
                entity.HasKey(e => e.MaKh).HasName("PK_Customers");

                entity.ToTable("KhachHang");

                entity.Property(e => e.MaKh)
                    .HasMaxLength(20)
                    .HasColumnName("MaKH");

                entity.Property(e => e.DiaChi).HasMaxLength(60);
                entity.Property(e => e.DienThoai).HasMaxLength(24);
                entity.Property(e => e.Email).HasMaxLength(50);

                entity.Property(e => e.Hinh)
                    .HasMaxLength(50)
                    .HasDefaultValue("Photo.gif");

                entity.Property(e => e.HoTen).HasMaxLength(50);
                entity.Property(e => e.MatKhau).HasMaxLength(50);
                entity.Property(e => e.NewsletterToken).HasMaxLength(100);

                entity.Property(e => e.NgaySinh)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");

                entity.Property(e => e.RandomKey)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                // Relationship with Role
                entity.HasOne(d => d.Role)
                    .WithMany(p => p.KhachHangs)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);

                entity.ToTable("Roles");

                entity.Property(e => e.RoleName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Description)
                    .HasMaxLength(200);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}