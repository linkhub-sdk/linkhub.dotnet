using System;

namespace Linkhub
{
    public class LinkhubException : Exception
    {
        public LinkhubException()
            : base()
        {
        }
        public LinkhubException(long code, String Message) : base(Message)
        {
            this._code = code;
        }

        private long _code;

        public long code
        {
            get { return _code; }
        }
        
    }
}
