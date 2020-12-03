namespace DShop.Services.Discounts.Dto
{
    public class DiscountDetailsDto
    {
        public CustomerDto Customer { get; set; }
        //We can inherits from DiscountDto but because favor of composition over inheritance use composition
        public DiscountDto Discount { get; set; }
    }
}