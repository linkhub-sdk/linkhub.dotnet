using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Linkhub
{
    [DataContract]
    public class Token
    {
        private String _session_token;
        private String _serviceID;
        private String _linkID;
        private String _usercode;
        private String _ipaddress;
        private String _expiration;
        private List<String> _scope;

        [DataMember]
        public String session_token
        {
            get { return _session_token; }
            set { _session_token = value; }
        }
        [DataMember]
        public String serviceID
        {
            get { return _serviceID; }
            set { _serviceID = value; }
        }
        [DataMember]
        public String linkID
        {
            get { return _linkID; }
            set { _linkID = value; }
        }
        [DataMember]
        public String usercode
        {
            get { return _usercode; }
            set { _usercode = value; }
        }
        [DataMember]
        public String expiration
        {
            get { return _expiration; }
            set { _expiration = value; }
        }
        [DataMember]
        public String ipaddress
        {
            get { return _ipaddress; }
            set { _ipaddress = value; }
        }
        [DataMember]
        public List<String> scope
        {
            get { return _scope; }
            set { _scope = value; }
        }
    }
}
