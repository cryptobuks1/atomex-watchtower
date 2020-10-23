using System.Net;

namespace Atomex.WatchTower.Entities
{
    public class Error
    {
        /// <summary>
        /// Error code
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }

        public Error() { }

        public Error(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public Error(HttpStatusCode status, string message)
        {
            Code = (int)status;
            Message = message;
        }
    }
}