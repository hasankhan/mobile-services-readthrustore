using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json.Linq;

namespace MobileServiceReadthruStore
{
    public sealed class NullSyncHandler : IMobileServiceSyncHandler
    {
        private static Task<JObject> success = Task.FromResult<JObject>(null);

        public Task<JObject> ExecuteTableOperationAsync(IMobileServiceTableOperation operation)
        {
            return success;
        }

        public Task OnPushCompleteAsync(MobileServicePushCompletionResult result)
        {
            return success;
        }
    }
}
