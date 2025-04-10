using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.OLAPModels;

public partial class OlapstoreTransactionDbContext : DbContext
{
    public OlapstoreTransactionDbContext()
    {
    }

    public OlapstoreTransactionDbContext(DbContextOptions<OlapstoreTransactionDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DimDate> DimDates { get; set; }

    public virtual DbSet<DimDepartment> DimDepartments { get; set; }

    public virtual DbSet<DimMonth> DimMonths { get; set; }

    public virtual DbSet<DimProduct> DimProducts { get; set; }

    public virtual DbSet<MonthlySalesAgg> MonthlySalesAggs { get; set; }

    public virtual DbSet<SalesFact> SalesFacts { get; set; }

    public virtual DbSet<TransactionType> TransactionTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)=> optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=OLAPStoreTransactionDB;Username=postgres;Password=admin");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DimDate>(entity =>
        {
            entity.HasKey(e => e.DateId).HasName("dim_date_pkey");

            entity.ToTable("dim_date");

            entity.Property(e => e.DateId).HasColumnName("date_id");
            entity.Property(e => e.Day).HasColumnName("day");
            entity.Property(e => e.DayOfWeek)
                .HasMaxLength(10)
                .HasColumnName("day_of_week");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<DimDepartment>(entity =>
        {
            entity.HasKey(e => e.DeptCode).HasName("dim_department_pkey");

            entity.ToTable("dim_department");

            entity.Property(e => e.DeptCode)
                .HasMaxLength(50)
                .HasColumnName("dept_code");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.DeptName)
                .HasMaxLength(255)
                .HasColumnName("dept_name");
        });

        modelBuilder.Entity<DimMonth>(entity =>
        {
            entity.HasKey(e => e.MonthId).HasName("dim_month_pkey");

            entity.ToTable("dim_month");

            entity.Property(e => e.MonthId).HasColumnName("month_id");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<DimProduct>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("dim_product_pkey");

            entity.ToTable("dim_product");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Brand)
                .HasMaxLength(100)
                .HasColumnName("brand");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.Origin)
                .HasMaxLength(100)
                .HasColumnName("origin");
            entity.Property(e => e.ParentCategory)
                .HasMaxLength(100)
                .HasColumnName("parent_category");
            entity.Property(e => e.PreviousName1)
                .HasMaxLength(255)
                .HasColumnName("previous_name_1");
            entity.Property(e => e.PreviousName2)
                .HasMaxLength(255)
                .HasColumnName("previous_name_2");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("product_name");
        });

        modelBuilder.Entity<MonthlySalesAgg>(entity =>
        {
            entity.HasKey(e => new { e.MonthId, e.DeptCode }).HasName("monthly_sales_agg_pkey");

            entity.ToTable("monthly_sales_agg");

            entity.Property(e => e.MonthId).HasColumnName("month_id");
            entity.Property(e => e.DeptCode)
                .HasMaxLength(50)
                .HasColumnName("dept_code");
            entity.Property(e => e.AverageDiscount)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("average_discount");
            entity.Property(e => e.PercentCashless)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("percent_cashless");
            entity.Property(e => e.TotalQuantity).HasColumnName("total_quantity");
            entity.Property(e => e.TotalReturns)
                .HasDefaultValue(0)
                .HasColumnName("total_returns");
            entity.Property(e => e.TotalSales)
                .HasPrecision(10, 2)
                .HasColumnName("total_sales");

            entity.HasOne(d => d.DeptCodeNavigation).WithMany(p => p.MonthlySalesAggs)
                .HasForeignKey(d => d.DeptCode)
                .HasConstraintName("monthly_sales_agg_dept_code_fkey");

            entity.HasOne(d => d.Month).WithMany(p => p.MonthlySalesAggs)
                .HasForeignKey(d => d.MonthId)
                .HasConstraintName("monthly_sales_agg_month_id_fkey");
        });

        modelBuilder.Entity<SalesFact>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("sales_fact_pkey");

            entity.ToTable("sales_fact");

            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.DateId).HasColumnName("date_id");
            entity.Property(e => e.DeptCode)
                .HasMaxLength(50)
                .HasColumnName("dept_code");
            entity.Property(e => e.Discount)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("0")
                .HasColumnName("discount");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.TotalSales)
                .HasPrecision(10, 2)
                .HasColumnName("total_sales");
            entity.Property(e => e.TransactionTypeId).HasColumnName("transaction_type_id");

            entity.HasOne(d => d.Date).WithMany(p => p.SalesFacts)
                .HasForeignKey(d => d.DateId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sales_fact_date_id_fkey");

            entity.HasOne(d => d.DeptCodeNavigation).WithMany(p => p.SalesFacts)
                .HasForeignKey(d => d.DeptCode)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sales_fact_dept_code_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.SalesFacts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sales_fact_product_id_fkey");

            entity.HasOne(d => d.TransactionType).WithMany(p => p.SalesFacts)
                .HasForeignKey(d => d.TransactionTypeId)
                .HasConstraintName("sales_fact_transaction_type_id_fkey");
        });

        modelBuilder.Entity<TransactionType>(entity =>
        {
            entity.HasKey(e => e.TransactionTypeId).HasName("transaction_type_pkey");

            entity.ToTable("transaction_type");

            entity.Property(e => e.TransactionTypeId).HasColumnName("transaction_type_id");
            entity.Property(e => e.IsCashless).HasColumnName("is_cashless");
            entity.Property(e => e.IsReturned).HasColumnName("is_returned");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
