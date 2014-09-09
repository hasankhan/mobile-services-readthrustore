using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.Query;
using Microsoft.WindowsAzure.MobileServices.Sync;
using Newtonsoft.Json.Linq;

namespace MobileServiceReadthruStore
{
    /// <summary>
    /// An implementation of <see cref="IMobileServiceLocalStore"/> that acts like a local cache
    /// </summary>
    public sealed class ReadthruLocalStore : MobileServiceLocalStore
    {
        private IMobileServiceLocalStore store;
        private IMobileServiceClient client;

        public ReadthruLocalStore(IMobileServiceLocalStore store, IMobileServiceClient client)
        {
            this.store = store;
            this.client = client;
        }

        protected override async Task OnInitialize()
        {
            await base.OnInitialize();
            await this.store.InitializeAsync();
        }

        public override async Task<JObject> LookupAsync(string tableName, string id)
        {
            JObject result = await this.store.LookupAsync(tableName, id);
            if (result == null && !IsSystemTable(tableName))
            {
                // get data from remote table
                result = await this.client.GetTable(tableName).LookupAsync(id) as JObject;
                // add data to local store
                await this.store.UpsertAsync(tableName, new[] { result }, fromServer: true);
            }
            return result;
        }

        public override async Task<JToken> ReadAsync(MobileServiceTableQueryDescription query)
        {
            // first lookup in local store
            JToken result = await this.store.ReadAsync(query);
            JArray items = GetItems(result);

            // if local store does not have results
            if ((items == null || items.Count == 0) && !IsSystemTable(query.TableName))
            {
                // then lookup the server
                result = await this.client.GetTable(query.TableName).ReadAsync(query.ToODataString());
                items = GetItems(result);

                // insert the results in the local store
                await this.store.UpsertAsync(query.TableName, items.Cast<JObject>(), fromServer: true);
            }

            return result;
        }

        public override async Task DeleteAsync(string tableName, IEnumerable<string> ids)
        {
            // ignore operations on system tables as we are in readthru mode
            if (IsSystemTable(tableName))
            {
                return;
            }

            foreach (string id in ids)
            {
                await this.client.GetTable(tableName).DeleteAsync(new JObject() { { "id", id } });
            }
            await this.store.DeleteAsync(tableName, ids);

            // clear the sync queue
            await this.client.SyncContext.PushAsync();
        }

        public override async Task DeleteAsync(MobileServiceTableQueryDescription query)
        {
            // ignore operations on system tables as we are in readthru mode
            if (IsSystemTable(query.TableName))
            {
                return;
            }

            await this.store.DeleteAsync(query);
        }

        public override async Task UpsertAsync(string tableName, IEnumerable<JObject> items, bool fromServer)
        {
            // ignore operations on system tables as we are in readthru mode
            if (IsSystemTable(tableName))
            {
                return;
            }


            var remoteTable = this.client.GetTable(tableName);
            foreach (JObject item in items)
            {
                JObject localItem = item;
                bool tryInsert = false;
                try
                {
                    // we don't know if it exists on server or not so always do an update first
                    localItem = await remoteTable.UpdateAsync(item) as JObject;
                }
                catch (MobileServiceInvalidOperationException ex)
                {
                    if (ex.Response.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                    // if item does not exist on server, we will try to insert
                    tryInsert = true;
                }

                if (tryInsert)
                {
                    // you can modify the server script to insert on update if item does
                    // not exist and this line would not be necessary in that case
                    localItem = await remoteTable.InsertAsync(item) as JObject;
                }

                // upsert on local store
                await this.store.UpsertAsync(tableName, new[] { localItem }, fromServer);
            }

            // clear the sync queue
            await this.client.SyncContext.PushAsync();
        }

        private static JArray GetItems(JToken result)
        {
            var items = result as JArray;
            if (items == null && result != null)
            {
                items = result["results"] as JArray;
            }
            return items;
        }

        private static bool IsSystemTable(string tableName)
        {
            return tableName == MobileServiceLocalSystemTables.OperationQueue ||
                   tableName == MobileServiceLocalSystemTables.SyncErrors;
        }

        public void Dispose()
        {
            this.store.Dispose();
        }
    }
}
