namespace SumoLib.Errors
{

    public class SumoQueryException : System.Exception
    {

        public SumoQueryException(string message) : base(message) { 
            
        }
        public SumoQueryException(string code, string message) : base($"{code} - {message}") { 
            this.ErrorCode= code;
        }
        public SumoQueryException(string message, System.Exception inner) : base(message, inner) {
            
         }
        protected SumoQueryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public string ErrorCode { get;  }
    }

   
}