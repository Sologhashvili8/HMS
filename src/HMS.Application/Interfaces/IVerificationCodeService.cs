namespace HMS.Application.Interfaces;

public interface IVerificationCodeService
{
    string Generate(string purpose, string email);
    bool Verify(string purpose, string email, string code);
}
