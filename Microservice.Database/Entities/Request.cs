using System;
using System.Collections.Generic;
using System.Text;

namespace Microservice.Database.Entities
{
    public class Request : BaseEntity
    {
        public string RequestIdentifier { get; set; }
        public string DatastoreIdentifier { get; set; }
        public bool SuccessfullyRun { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string FilePath { get; set; }
    }
}
