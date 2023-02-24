using Microservice.Business.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microservice.Business.Business
{
    public interface IExportManagement
    {
        void AddRequest(RequestModel model, out string errorMessage);
        void ProcessRequest();
    }
}
