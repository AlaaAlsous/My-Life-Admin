using Microsoft.Extensions.Configuration;
using Moq;
using My_Life_Admin.Services;
using System.Net.Mail;

namespace My_Life_Admin_Backend.Tests.Services;

public class EmailServiceTests
{
    [Fact]
    public async Task SendReminderAsync_SendsCorrectEmail()
    {
        // Arrange
        var settings = new Dictionary<string, string?>
        {
            ["Email:FromEmail"] = "sender@example.com"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var emailSender = new Mock<IEmailSender>();

        MailMessage? sentMessage = null;

        emailSender
            .Setup(x => x.SendAsync(It.IsAny<MailMessage>()))
            .Callback<MailMessage>(message => sentMessage = message)
            .Returns(Task.CompletedTask);

        var service = new EmailService(
            configuration,
            emailSender.Object);

        // Act
        await service.SendReminderAsync(
            "recipient@example.com",
            "Test subject",
            "Test message");

        // Assert
        Assert.NotNull(sentMessage);
        Assert.Equal(
            "recipient@example.com",
            sentMessage.To[0].Address);
        Assert.Equal(
            "sender@example.com",
            sentMessage.From?.Address);
        Assert.Equal("Test subject", sentMessage.Subject);
        Assert.Equal("Test message", sentMessage.Body);

        emailSender.Verify(
            x => x.SendAsync(It.IsAny<MailMessage>()),
            Times.Once);
    }

    [Fact]
    public async Task SendReminderAsync_MissingFromEmail_ThrowsException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        var emailSender = new Mock<IEmailSender>();

        var service = new EmailService(
            configuration,
            emailSender.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SendReminderAsync(
                "recipient@example.com",
                "Test subject",
                "Test message"));

        emailSender.Verify(
            x => x.SendAsync(It.IsAny<MailMessage>()),
            Times.Never);
    }
}