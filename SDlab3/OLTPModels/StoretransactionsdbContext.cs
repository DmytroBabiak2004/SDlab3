using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SDlab3.OLTPModels;

public partial class StoretransactionsdbContext : DbContext
{
    public StoretransactionsdbContext()
    {
    }

    public StoretransactionsdbContext(DbContextOptions<StoretransactionsdbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=storetransactionsdb;Username=postgres;Password=admin");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.BrandId).HasName("brands_pkey");

            entity.ToTable("brands");

            entity.HasIndex(e => e.BrandName, "brands_brand_name_key").IsUnique();

            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.BrandName)
                .HasMaxLength(50)
                .HasColumnName("brand_name");
            entity.Property(e => e.Origin)
                .HasMaxLength(50)
                .HasColumnName("origin");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DeptCode).HasName("departments_pkey");

            entity.ToTable("departments");

            entity.Property(e => e.DeptCode)
                .HasMaxLength(20)
                .HasColumnName("dept_code");
            entity.Property(e => e.DeptName)
                .HasMaxLength(100)
                .HasColumnName("dept_name");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmpCode).HasName("employees_pkey");

            entity.ToTable("employees");

            entity.Property(e => e.EmpCode)
                .HasMaxLength(50)
                .HasColumnName("emp_code");
            entity.Property(e => e.EmpName)
                .HasMaxLength(100)
                .HasColumnName("emp_name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("products_pkey");

            entity.ToTable("products");

            entity.Property(e => e.ProductId)
                .ValueGeneratedNever()
                .HasColumnName("product_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.LastStockPrice)
                .HasPrecision(10, 2)
                .HasColumnName("last_stock_price");
            entity.Property(e => e.LastSupplyPrice)
                .HasPrecision(10, 2)
                .HasColumnName("last_supply_price");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("products_brand_id_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("products_category_id_fkey");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("product_categories_pkey");

            entity.ToTable("product_categories");

            entity.Property(e => e.CategoryId)
                .ValueGeneratedNever()
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("product_categories_parent_category_id_fkey");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.SaleId).HasName("sales_pkey");

            entity.ToTable("sales");

            entity.Property(e => e.SaleId).HasColumnName("sale_id");
            entity.Property(e => e.DeptCode)
                .HasMaxLength(20)
                .HasColumnName("dept_code");
            entity.Property(e => e.Discount)
                .HasPrecision(10, 2)
                .HasColumnName("discount");
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .HasColumnName("employee_code");
            entity.Property(e => e.IsCashless).HasColumnName("is_cashless");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ReturnReason)
                .HasMaxLength(100)
                .HasColumnName("return_reason");
            entity.Property(e => e.SaleDate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("sale_date");
            entity.Property(e => e.TransactionCode)
                .HasMaxLength(20)
                .HasColumnName("transaction_code");

            entity.HasOne(d => d.DeptCodeNavigation).WithMany(p => p.Sales)
                .HasForeignKey(d => d.DeptCode)
                .HasConstraintName("sales_dept_code_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.Sales)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("sales_product_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
