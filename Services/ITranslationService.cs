using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Orchard;
using System.ServiceModel;

namespace EXPEDIT.Flow.Services
{
    [ServiceContract]
    public interface ITranslationService : IDependency
    {
        [OperationContract]
        bool CreateLocale(EXPEDIT.Flow.ViewModels.LocaleViewModel m);
        [OperationContract]
        bool DeleteLocale(EXPEDIT.Flow.ViewModels.LocaleViewModel m);
        [OperationContract]
        bool DeleteTranslation(EXPEDIT.Flow.ViewModels.TranslationViewModel m);
        [OperationContract]
        bool GetLocale(EXPEDIT.Flow.ViewModels.LocaleViewModel m);
        [OperationContract]
        bool GetTranslation(EXPEDIT.Flow.ViewModels.TranslationViewModel m);
        [OperationContract]
        bool UpdateLocale(EXPEDIT.Flow.ViewModels.LocaleViewModel m);
        [OperationContract]
        bool UpdateTranslation(EXPEDIT.Flow.ViewModels.TranslationViewModel m);
    }
}
