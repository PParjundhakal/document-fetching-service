using Microsoft.Extensions.Configuration;
using Microservice.Business.Models;
using Microservice.Business.Repositories;
using Microservice.Database.Entities;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using PsychPress.Utilities.Logging;
using PsychPress.SecureDataStore.Clients;
using Microservice.Business.Extensions;
using PsychPress.SecureDataStore.Models;

namespace Microservice.Business.Business.Concrete
{
    public class ExportManagement : IExportManagement
    {
        private readonly IDatabaseRepository<Request> _request;
        private readonly SecureDataStoreClient _secureDataStore;
        private readonly string _outputDirectory;

        public ExportManagement(
            IDatabaseRepository<Request> request,
            IConfiguration configuration)
        {
            _request = request;
            _secureDataStore = new SecureDataStoreClient(configuration.GetValue<string>("SecureDataStoreApiServer"));
            _outputDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
               configuration.GetValue<string>("OutputDirectory"));
        }

        public void AddRequest(RequestModel model, out string errorMessage)
        {
            errorMessage = null;

            Log.Information("Creating document fetch requests.");

            foreach (var datastoreIdentifier in model.DatastoreIdentifiers)
            {
                var request = new Request()
                {
                    DatastoreIdentifier = datastoreIdentifier,
                    RequestIdentifier = Guid.NewGuid().ToString()
                };

                _request.Create(request);

                Log.Information("Successfully created document fetch request for identifier {0}.", datastoreIdentifier);
            }
            return;
        }

        public void ProcessRequest()
        {
            var allRequests = _request.Read(m => m.CompletedAt is null)
                .OrderBy(m => m.CreatedAt);

            var request = allRequests.Where(m => true).FirstOrDefault();

            if (request == default)
            {
                Log.Information("The request queue is empty. No request was processed.");
                return;
            }


            Log.Information("Start processing the next document fetching request in queue.");

            if (!Directory.Exists(_outputDirectory))
                Directory.CreateDirectory(_outputDirectory);

            var storedFilePath = WriteToInputFolder(request);

            if (storedFilePath == null)
            {
                Log.Error("Failed to write file in folder for request {0}.", request.RequestIdentifier);
                UpdateFailedRequest(request);
                return;
            }

            request.SuccessfullyRun = true;
            request.CompletedAt = DateTime.UtcNow;
            request.FilePath = storedFilePath;
            _request.Update(request);

            Log.Information("Successfully completed processing a document fetching request {0}", request.RequestIdentifier);
        }

        private string WriteToInputFolder(Request request)
        {
            try
            {
                var inputFile = LoadFileFromSDS(request.DatastoreIdentifier);
                if (inputFile is null)
                {
                    Log.Error("Failed to read input file from Secure Data Store");
                    UpdateFailedRequest(request);
                    return null;
                }
                using (StreamWriter sw = File.CreateText(Path.Combine(_outputDirectory,
                    request.RequestIdentifier + "_" + inputFile.FileName + ".json")))
                {
                    sw.Write(inputFile.Content);
                }
                return Path.Combine(_outputDirectory, request.RequestIdentifier + "_" + inputFile.FileName + ".json");
            }
            catch (Exception ex)
            {
                Log.Error("Caught exception while writing file in folder for request {0}.", request.RequestIdentifier);
                return null;
            }
        }

        private void UpdateFailedRequest(Request request)
        {
            request.SuccessfullyRun = false;
            request.CompletedAt = DateTime.UtcNow;
            _request.Update(request);
        }


        private StoreFileModel LoadFileFromSDS(string datastoreIdentifier)
        {
            if (datastoreIdentifier is null)
                return null;

            var fileModel = _secureDataStore.GetFileByIdentifier(datastoreIdentifier);

            if (fileModel?.Content is null)
            {
                Log.Error("Failed to load data from secure data store");
                return null;
            }

            fileModel.Content = fileModel.Content.Base64Decode();
            return fileModel;
        }

    }
}
