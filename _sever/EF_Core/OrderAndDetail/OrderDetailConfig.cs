using _sever.EF_Core.CuisineMenu;
using Microsoft.EntityFrameworkCore;

namespace _sever.EF_Core.OrderAndDetail
{
    public class OrderDetailConfig : IEntityTypeConfiguration<OrderDetail>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<OrderDetail> builder)
        {
            builder.ToTable("T_OrderDetail");
            builder.HasOne(orderDetail=>orderDetail.Order).WithMany(order=>order.OrderDetails).HasForeignKey(orderDetail=>orderDetail.OrderId);
            builder.Property(orderDetail => orderDetail.CuisinePrice).HasColumnType("decimal(7, 2)");
            builder.Property(orderDetail => orderDetail.Amout).HasColumnType("decimal(7, 2)");
        }
    }
}
