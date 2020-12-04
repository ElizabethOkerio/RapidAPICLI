using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using RapidApi.Cli.Common;
using RapidApi.Cli.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RapidApi.Local
{
    class ServiceUpdater
    {
        ImageCredentials image;

        public ServiceUpdater(ImageCredentials image)
        {
            this.image = image;
        }

        public void UpdateService()
        {
            var hosts = new Hosts().Discover();
            var host = hosts.First().Host;
            var response = host.Login(image.Server, image.Username, image.Password);
            if (!response.Success)
            {
                throw new Exception(response.Error);
            }

            response = host.Pull(image.Name);
            if (!response.Success)
            {
                throw new RapidApiException(response.Error);
            }
        }
    }
}
