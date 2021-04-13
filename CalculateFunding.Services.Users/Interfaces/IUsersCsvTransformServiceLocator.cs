namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUsersCsvTransformServiceLocator
    {
        IUsersCsvTransform GetService(string jobDefinitionName);
    }
}
