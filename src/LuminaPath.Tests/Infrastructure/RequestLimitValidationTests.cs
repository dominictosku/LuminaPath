using LuminaPath.Core.Dtos;
using LuminaPath.Infrastructure.Controllers;
using LuminaPath.Infrastructure.Services.AiChat;
using LuminaPath.Infrastructure.Validators;

namespace LuminaPath.Tests.Infrastructure;

public class RequestLimitValidationTests
{
    [Fact]
    public void ChatRequestValidator_RejectsOversizedMessage()
    {
        var validator = new ChatRequestValidator();
        var request = new ChatRequest
        {
            Messages =
            [
                new ChatRequestMessage
                {
                    Role = "user",
                    Content = new string('x', ChatRequestLimits.MaxMessageCharacters + 1)
                }
            ]
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains(nameof(ChatRequestMessage.Content)));
    }

    [Fact]
    public void ChatRequestValidator_RejectsTooManyMessages()
    {
        var validator = new ChatRequestValidator();
        var request = new ChatRequest
        {
            Messages = Enumerable.Range(0, ChatRequestLimits.MaxMessages + 1)
                .Select(_ => new ChatRequestMessage { Role = "user", Content = "hello" })
                .ToList()
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains(ChatRequestLimits.MaxMessages.ToString()));
    }

    [Fact]
    public void SendDirectMessageValidator_RejectsOversizedMessage()
    {
        var validator = new SendDirectMessageDtoValidator();
        var request = new SendDirectMessageDto
        {
            RecipientId = "recipient",
            Content = new string('x', DirectMessageLimits.MaxMessageCharacters + 1)
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SendDirectMessageDto.Content));
    }

    [Fact]
    public void SteamIdentifierValidator_RejectsOversizedIdentifier()
    {
        var validator = new SteamIdentifierRequestValidator();
        var request = new SteamIdentifierRequest
        {
            Identifier = new string('x', SteamIdentifierRequest.MaxIdentifierLength + 1)
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SteamIdentifierRequest.Identifier));
    }
}
