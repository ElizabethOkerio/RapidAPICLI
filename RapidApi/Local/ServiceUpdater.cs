using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RapidApi.Local
{
    public class ServiceUpdater
    {
        public void UpdateService()
        {
            var image = "rapidapiregistry.azurecr.io/rapidapimockserv:latest";
            var hosts = new Hosts().Discover();
            var host = hosts.First().Host;
            var response = host.Login("rapidapiregistry.azurecr.io", "rapidapiregistry", "3RSdU=zGg=AIvjesICqISXdBbMiwYigk");
            if (!response.Success)
            {
                throw new Exception(response.Error);
            }

            Console.WriteLine("Checking for core updates...");
            response = host.Pull(image);
            if (!response.Success)
            {
                throw new Exception(response.Error);
            }
        }
    }
}
