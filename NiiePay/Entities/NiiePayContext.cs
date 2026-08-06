using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NiiePay.Entities;

public partial class NiiePayContext : DbContext
{
    public NiiePayContext()
    {
    }

    public NiiePayContext(DbContextOptions<NiiePayContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<GiaoDich> GiaoDiches { get; set; }

    public virtual DbSet<NganHang> NganHangs { get; set; }

    public virtual DbSet<SoTietKiem> SoTietKiems { get; set; }

    public DbSet<LaiSuatKyHan> LaiSuatKyHan { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.; Database=NiiePay; Integrated Security=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.SoTaiKhoan);

            entity.HasIndex(e => e.Cccd, "UQ_CCCD").IsUnique();

            entity.HasIndex(e => e.SoDienThoai, "UQ_SoDienThoai").IsUnique();

            entity.Property(e => e.SoTaiKhoan)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Cccd)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CCCD");
            entity.Property(e => e.HoTenChuThe).HasMaxLength(100);
            entity.Property(e => e.MaNganHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.SoDuKhaDung).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ThoiGianTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaNganHangNavigation).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.MaNganHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Accounts_NganHang");
        });

        modelBuilder.Entity<GiaoDich>(entity =>
        {
            entity.HasKey(e => e.MaGiaoDich);

            entity.ToTable("GiaoDich");

            entity.Property(e => e.MaGiaoDich)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MaNganHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NoiDung).HasMaxLength(255);
            entity.Property(e => e.SoDuSauGiaoDich).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaiKhoanGui)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TaiKhoanNhan)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ThoiGian)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.Property(e => e.TaiKhoanSoHuu)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LoaiGiaoDich)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.MaNganHangNavigation).WithMany(p => p.GiaoDiches)
                .HasForeignKey(d => d.MaNganHang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GiaoDich_NganHang_Nhan");

            entity.HasOne(d => d.TaiKhoanGuiNavigation).WithMany(p => p.GiaoDiches)
                .HasForeignKey(d => d.TaiKhoanGui)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GiaoDich_Accounts_Gui");
        });

        modelBuilder.Entity<NganHang>(entity =>
        {
            entity.HasKey(e => e.MaNganHang);

            entity.ToTable("NganHang");

            entity.Property(e => e.MaNganHang)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TenNganHang).HasMaxLength(100);
        });

        modelBuilder.Entity<SoTietKiem>(entity =>
        {
            entity.HasKey(e => e.MaSoTietKiem);

            entity.ToTable("SoTietKiem");

            entity.Property(e => e.MaSoTietKiem)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LaiSuat).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.NgayMoSo).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SoTaiKhoan)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SoTienGui).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TuDongGiaHan).HasDefaultValue(true);

            entity.HasOne(d => d.SoTaiKhoanNavigation).WithMany(p => p.SoTietKiems)
                .HasForeignKey(d => d.SoTaiKhoan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SoTietKiem_Accounts");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
