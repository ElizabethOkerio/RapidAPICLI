using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RapidApi.Local
{
    class LocalRunner: IDisposable
    {
        private bool disposedValue;

        public int Port { get; set; }
        public string SchemaPath { get; set; }

        public Action<string, int> BeforeRestart { get; set; }
        public Action<string, int> AfterRestart { get; set; }

        public LocalRunner(string schemaPath, int port)
        {
            Port = port;
            SchemaPath = schemaPath;
            watcher = new FileSystemWatcher();

        }

        public void Start()
        {
            watcher.Path = Path.GetDirectoryName(SchemaPath);
            watcher.Filter = Path.GetFileName(SchemaPath);
            watcher.NotifyFilter = NotifyFilters.Security;
            watcher.Changed += OnSchemaChange;
            RestartContainer();

            watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            Dispose();
        }

        private void RestartContainer()
        {
            container?.Dispose();
            container = null;
            
            container = new Builder()
                .UseContainer()
                .UseImage("rapidapiregistry.azurecr.io/rapidapimockserv:latest")
                .WithCredential("rapidapiregistry.azurecr.io", "rapidapiregistry", "lfd34HcYycIg+rttO0D5AeZjZL2=pqZt")
                .ExposePort(Port, 80)
                .CopyOnStart(SchemaPath, "/app/Project.csdl")
                .Build()
                .Start();
            container.StopOnDispose = true;
            container.RemoveOnDispose = true;
        }

        private void OnSchemaChange(object source, FileSystemEventArgs e)
        {
            Console.WriteLine($"Change detected {source} {e.Name} {e.ChangeType}");
            BeforeRestart?.Invoke(SchemaPath, Port);
            RestartContainer();
            AfterRestart?.Invoke(SchemaPath, Port);
        }



        private FileSystemWatcher watcher;
        private IContainerService container;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Console.WriteLine("Removing container");
                    container?.Stop();
                    container?.Remove();
                    container?.Dispose();
                    watcher?.Dispose();
                    container = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LocalRunner()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
