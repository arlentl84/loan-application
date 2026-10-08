using Lending.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        
        builder.Property(c => c.Ssn)
            .HasConversion(ssn => ssn.Value, value => Ssn.Create(value))
            .HasMaxLength(9)
            .IsRequired();
        builder.HasIndex(c => c.Ssn).IsUnique();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Street).HasMaxLength(200).IsRequired();
        builder.Property(c => c.City).HasMaxLength(100).IsRequired();
        builder.Property(c => c.State).HasMaxLength(2).IsRequired();
        builder.Property(c => c.ZipCode).HasMaxLength(10).IsRequired();
        builder.Property(c => c.CompanyName).HasMaxLength(200).IsRequired();
    }
}
