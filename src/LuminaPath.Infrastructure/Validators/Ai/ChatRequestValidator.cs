using FluentValidation;
using LuminaPath.Infrastructure.Services.AiChat;

namespace LuminaPath.Infrastructure.Validators;

public sealed class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "user",
        "assistant"
    };

    public ChatRequestValidator()
    {
        RuleFor(request => request.Messages)
            .NotEmpty()
            .WithMessage("At least one chat message is required.")
            .Must(messages => messages is null || messages.Count <= ChatRequestLimits.MaxMessages)
            .WithMessage($"Chat requests can include at most {ChatRequestLimits.MaxMessages} messages.");

        RuleFor(request => request)
            .Must(request => request.Messages is null
                || request.Messages.Sum(message => message.Content?.Length ?? 0) <= ChatRequestLimits.MaxTotalCharacters)
            .WithMessage($"Chat requests can include at most {ChatRequestLimits.MaxTotalCharacters} total characters.");

        RuleForEach(request => request.Messages)
            .ChildRules(message =>
            {
                message.RuleFor(item => item.Role)
                    .Must(role => !string.IsNullOrWhiteSpace(role) && AllowedRoles.Contains(role))
                    .WithMessage("Message role must be user or assistant.");

                message.RuleFor(item => item.Content)
                    .NotEmpty()
                    .WithMessage("Message content is required.")
                    .MaximumLength(ChatRequestLimits.MaxMessageCharacters)
                    .WithMessage($"Message content can include at most {ChatRequestLimits.MaxMessageCharacters} characters.");
            });
    }
}
