using System;
using System.Collections.Generic;
using System.Text;

namespace RapidApi.Cli.Common
{
    class RapidApiException: Exception
    {
        public RapidApiException() : base() { }
        public RapidApiException(string message) : base(message) { }
    }
}
