using FluentValidation;
using IdentityFramework;

namespace _sever.Validator
{
    public class AddUserValidator: AbstractValidator<UserVo>
    {
        public AddUserValidator() {
            RuleFor(user=> user.UserName).NotNull().NotEmpty()
                .WithMessage("用户名不能为空");
            RuleFor(user => user.Password)
                .MinimumLength(4).WithMessage("密码不能小于4位")
                .MaximumLength(8).WithMessage("密码不能超过8位");
            RuleFor(user => user.PhoneNumber).Matches(@"^\d{11}$")
                .WithMessage("电话号码格式不正确");
            RuleFor(user => user.Email).EmailAddress();
        }
    }
}
