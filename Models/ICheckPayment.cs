using CookComputing.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EXPEDIT.License.Models
{
    [XmlRpcUrl("http://miningappstore.com/xmlrpc")]
    public interface ICheckPayment : IXmlRpcProxy
    {
        [XmlRpcMethod("transactions.validateModelContact")]
        string ValidateModelContact(string modelID, string contactID);
    }
}