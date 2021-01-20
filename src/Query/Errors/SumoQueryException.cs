namespace SumoLib.Errors
{

    public class SumoQueryException : System.Exception
    {

        public SumoQueryException(SumoQueryErrors code) : base(code.ToString()) { 
            this.Code= code;
        }
        public SumoQueryException(SumoQueryErrors code, System.Exception inner) : base(code.ToString(), inner) {

            this.Code= code;
         }
        protected SumoQueryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        public SumoQueryErrors Code { get;  }
    }

    public enum SumoQueryErrors
    {
        AUTH_FAILED,
        INVALID_QUERY,
        RESULTS_TOO_LARGE,

        INVALID_BATCH_ID,
        TIMEOUT_ON_PAUSED_QUERY

    }
}