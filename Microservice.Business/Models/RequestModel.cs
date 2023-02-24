using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microservice.Business.Models
{
    public class RequestModel
    {
        [Required]
        public List<string> DatastoreIdentifiers { get; set; }
    }
}
