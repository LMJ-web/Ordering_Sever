using _sever.EF_Core.CuisineMenu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _sever.EF_Core.OrderAndDetail
{
    public class OrderConfig : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("T_Order");
            builder.Property(order => order.Total).HasColumnType("decimal(7, 2)");
            builder.Property(order => order.PayState).HasColumnType("int");
        }
    }
}
