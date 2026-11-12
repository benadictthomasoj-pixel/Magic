using FluentValidation;
using Magic.Contracts.Accounting;

namespace Magic.Application.Accounting.Validation;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    private static readonly HashSet<string> AllowedClasses = ["Asset", "Liability", "Equity", "Revenue", "Expense"];
    private static readonly HashSet<string> AllowedControlTypes =
    [
        "CustomerControl",
        "VendorControl",
        "GstControl",
        "BankControl",
        "CashControl",
        "InventoryControl"
    ];

    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty().WithMessage("Account group identifier is required.");
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Account code is required.")
            .MaximumLength(30).WithMessage("Account code must not exceed 30 characters.")
            .Matches(@"^[A-Za-z0-9\-_.]+$").WithMessage("Account code must be alphanumeric with optional hyphens or dots.");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(150).WithMessage("Account name must not exceed 150 characters.");
        RuleFor(x => x.RootClass)
            .NotEmpty().WithMessage("Root class is required.")
            .Must(rc => AllowedClasses.Contains(rc)).WithMessage("Root class must be Asset, Liability, Equity, Revenue, or Expense.");
        RuleFor(x => x.ControlAccountType)
            .Must(ct => string.IsNullOrWhiteSpace(ct) || AllowedControlTypes.Contains(ct))
            .WithMessage("Control account type must be one of: CustomerControl, VendorControl, GstControl, BankControl, CashControl, InventoryControl.");
    }
}

public sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(150).WithMessage("Account name must not exceed 150 characters.");
    }
}

public sealed class CreateJournalDraftRequestValidator : AbstractValidator<CreateJournalDraftRequest>
{
    public CreateJournalDraftRequestValidator()
    {
        RuleFor(x => x.FiscalPeriodId).NotEmpty().WithMessage("Fiscal period is required.");
        RuleFor(x => x.VoucherDate).NotEmpty().WithMessage("Voucher date is required.");
        RuleFor(x => x.Narration)
            .NotEmpty().WithMessage("Journal narration is required.")
            .MaximumLength(1000).WithMessage("Narration must not exceed 1000 characters.");
        RuleFor(x => x.Lines)
            .NotNull().WithMessage("Journal lines are required.")
            .Must(lines => lines != null && lines.Count >= 2).WithMessage("A journal entry requires at least 2 lines (debit and credit).");
        RuleForEach(x => x.Lines).SetValidator(new JournalLineInputDtoValidator());
    }
}

public sealed class JournalLineInputDtoValidator : AbstractValidator<JournalLineInputDto>
{
    public JournalLineInputDtoValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("Line account is required.");
        RuleFor(x => x)
            .Must(line => (line.DebitPaise > 0 && line.CreditPaise == 0) || (line.DebitPaise == 0 && line.CreditPaise > 0))
            .WithMessage("A line must be strictly debit OR credit, never both or neither.");
        RuleFor(x => x.DebitPaise).GreaterThanOrEqualTo(0).WithMessage("Debit amount must be non-negative.");
        RuleFor(x => x.CreditPaise).GreaterThanOrEqualTo(0).WithMessage("Credit amount must be non-negative.");
        RuleFor(x => x.Narration).MaximumLength(500).WithMessage("Line narration must not exceed 500 characters.");
    }
}

public sealed class PostVoucherRequestValidator : AbstractValidator<PostVoucherRequest>
{
    public PostVoucherRequestValidator()
    {
        RuleFor(x => x.BranchCode).NotEmpty().MaximumLength(20).WithMessage("Branch code is required.");
        RuleFor(x => x.FyCode).NotEmpty().MaximumLength(20).WithMessage("Fiscal year code is required.");
    }
}

public sealed class ReverseVoucherRequestValidator : AbstractValidator<ReverseVoucherRequest>
{
    public ReverseVoucherRequestValidator()
    {
        RuleFor(x => x.BranchCode).NotEmpty().MaximumLength(20).WithMessage("Branch code is required.");
        RuleFor(x => x.FyCode).NotEmpty().MaximumLength(20).WithMessage("Fiscal year code is required.");
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reversal reason is required.")
            .MaximumLength(500).WithMessage("Reversal reason must not exceed 500 characters.");
    }
}
