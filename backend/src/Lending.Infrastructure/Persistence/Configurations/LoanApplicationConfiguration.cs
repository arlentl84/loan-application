using Lending.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Persistence.Configurations;

public sealed class LoanApplicationConfiguration : IEntityTypeConfiguration<LoanApplication>
{
    public void Configure(EntityTypeBuilder<LoanApplication> builder)
    {
        builder.ToTable("loan_applications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.RequestedAmount).HasPrecision(18, 2);
        
        builder.HasOne<Customer>()
            .WithOne()
            .HasForeignKey<LoanApplication>(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
