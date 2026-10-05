using CafeManagement.Application.Auth.Commands;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace CafeManagement.Application.Tests.Security;

public class JwtValidationTests
{
    private readonly RegisterCommandValidator _registerValidator;
    private readonly LoginCommandValidator _loginValidator;
    private readonly RefreshTokenCommandValidator _refreshTokenValidator;
    private readonly ChangePasswordCommandValidator _changePasswordValidator;
    private readonly ForgotPasswordCommandValidator _forgotPasswordValidator;
    private readonly ResetPasswordCommandValidator _resetPasswordValidator;

    public JwtValidationTests()
    {
        _registerValidator = new RegisterCommandValidator();
        _loginValidator = new LoginCommandValidator();
        _refreshTokenValidator = new RefreshTokenCommandValidator();
        _changePasswordValidator = new ChangePasswordCommandValidator();
        _forgotPasswordValidator = new ForgotPasswordCommandValidator();
        _resetPasswordValidator = new ResetPasswordCommandValidator();
    }

    [Fact]
    public void LoginCommand_WithInvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var command = new LoginCommand("", "validpassword123");

        // Act
        ValidationResult result = _loginValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        // Email validates both NotEmpty and EmailAddress, so 2 errors for empty email
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().OnlyContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void LoginCommand_WithInvalidPassword_ShouldFailValidation()
    {
        // Arrange
        var command = new LoginCommand("test@example.com", ""); // Empty password

        // Act
        ValidationResult result = _loginValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].PropertyName.Should().Be("Password");
    }

    [Fact]
    public void RegisterCommand_WithMissingFields_ShouldFailValidation()
    {
        // Arrange
        var command = new RegisterCommand("", "", "");

        // Act
        ValidationResult result = _registerValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        // Now validates all fields including password complexity rules
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
    }

    [Fact]
    public void RefreshTokenCommand_WithEmptyToken_ShouldFailValidation()
    {
        // Arrange
        var command = new RefreshTokenCommand("");

        // Act
        ValidationResult result = _refreshTokenValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].PropertyName.Should().Be("RefreshToken");
    }

    [Fact]
    public void ChangePasswordCommand_WithInvalidCurrentPassword_ShouldFailValidation()
    {
        // Arrange
        var command = new ChangePasswordCommand("", "newpassword123"); // Empty current password

        // Act
        ValidationResult result = _changePasswordValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        // CurrentPassword validates NotEmpty and complexity rules
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
        // NewPassword is valid, so no errors for it
    }

    [Fact]
    public void ForgotPasswordCommand_WithInvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var command = new ForgotPasswordCommand(""); // Empty email triggers both NotEmpty and EmailAddress

        // Act
        ValidationResult result = _forgotPasswordValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        // Email validates both NotEmpty and EmailAddress
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().OnlyContain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void ResetPasswordCommand_WithInvalidEmail_ShouldFailValidation()
    {
        // Arrange
        var command = new ResetPasswordCommand("invalid-email", "token123", "newpassword123");

        // Act
        ValidationResult result = _resetPasswordValidator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        // Email validates both NotEmpty and EmailAddress, plus NewPassword validates complexity
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}