using EBISX_POS.API.Models;
using EBISX_POS.API.Services.DTO.Order;
using System.Collections.ObjectModel;

namespace EBISX_POS.API.Services.Interfaces
{
    public interface IOrder
    {
        Task<(bool, string)> AddOrderItem(AddOrderDTO addOrder);
        Task<(bool, string)> AddCurrentOrderVoid(AddCurrentOrderVoidDTO voidOrder);
        Task<(bool, string)> AddPwdScDiscount(AddPwdScDiscountDTO addPwdScDiscount);
        Task<(bool, string)> AddSinglePwdScDiscount(bool isPWD, string oscaNum, string elligibleName, string managerEmail, string cashierEmail);
        Task<(bool, string)> AddOtherDiscount(AddOtherDiscountDTO addOtherDiscount);


        Task<(bool, string)> VoidOrderItem(VoidOrderItemDTO voidOrder);
        Task<(bool, string)> EditQtyOrderItem(EditOrderItemQuantityDTO editOrder);

        Task<(bool IsSuccess, string Message, FinalizeOrderResponseDTO? Response)> FinalizeOrder(FinalizeOrderDTO finalizeOrder);

        Task<(bool, string)> CancelCurrentOrder(string cashierEmail, string managerEmail);
        Task<(bool, string)> RefundOrder(string managerEmail, long invoiceNumber);
        Task<(bool IsSuccess, string Message)> RefundItemOrder(string managerEmail, long invoiceNumber, List<Item> items);
        Task<List<Item>> GetItems(long invoiceNumber);

        Task<(bool, string)> PromoDiscount(string cashierEmail, string managerEmail, string promoCode);
        Task<(bool, string)> AvailCoupon(string cashierEmail, string managerEmail, string couponCode);

        Task<List<GetCurrentOrderItemsDTO>> GetCurrentOrderItems(string cashierEmail);
        Task<List<string>> GetElligiblePWDSCDiscount(string cashierEmail);

    }
}
