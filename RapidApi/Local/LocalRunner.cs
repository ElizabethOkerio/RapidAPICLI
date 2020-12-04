using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RapidApi.Common.Models;
using RapidApi.Cli.Common;
using RapidApi.Cli.Common.Models;

namespace RapidApi.Local
{
    class LocalRunner: IDisposable
    {
        private bool disposedValue;
        private bool firstStart = true;

        private ImageCredentials image;

        public int Port { get; set; }
        public string SchemaPath { get; set; }
        public Action<string, int> BeforeRestart { get; set; }
        public Action<string, int> AfterRestart { get; set; }
        public Action<Exception> OnError { get; set; }
        public Action OnSchemaChange { get; set; }
        public Action OnTerminate { get; set; }

        public ProjectRunArgs ProjectRunArgs { get; set; }



        public LocalRunner(string schemaPath, int port, ProjectRunArgs args, ImageCredentials image)
        {
            Port = port;
            ProjectRunArgs = args;
            SchemaPath = schemaPath;
            this.image = image;
            watcher = new FileSystemWatcher();
        }

        public void Start()
        {
            watcher.Path = Path.GetDirectoryName(SchemaPath);
            watcher.Filter = Path.GetFileName(SchemaPath);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += HandleSchemaChange;
            RestartContainer();

            watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            Dispose();
        }

        private bool RestartContainer()
        {
            if (!firstStart)
            {
                BeforeRestart?.Invoke(SchemaPath, Port);
            }

            container?.Dispose();
            container = null;

            try
            {
                SchemaValidator.ValidateSchema(SchemaPath);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                return false;
            }


            var builder = new Builder()
                .UseContainer()
                .UseImage(image.Name)
                .WithCredential(image.Server, image.Username, image.Password)
                .ExposePort(Port, 80)
                .KeepRunning()
                .CopyOnStart(SchemaPath, "/app/Project.csdl");

            if (ProjectRunArgs.SeedData)
            {
                builder.WithEnvironment("SEED_DATA=true");
            }

            container = builder
                .Build();

            container.StopOnDispose = true;
            container.RemoveOnDispose = true;
            container.Start();

            firstStart = false;

            AfterRestart?.Invoke(SchemaPath, Port);

            return true;
        }

        private void HandleSchemaChange(object source, FileSystemEventArgs e)
        {
            OnSchemaChange?.Invoke();
            RestartContainer();
        }



        private FileSystemWatcher watcher;
        private IContainerService container;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnTerminate?.Invoke();
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
