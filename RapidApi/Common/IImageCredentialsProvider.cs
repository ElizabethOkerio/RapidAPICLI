using RapidApi.Cli.Common.Models;
using System.Threading.Tasks;

namespace RapidApi.Cli.Common
{
    interface IImageCredentialsProvider
    {
        Task<ImageCredentials> GetCredentials();
    }
}
