MobileServiceReadthruStore
==========================

A sample readthru local store implementation for Azure Mobile Services client SDK that allows you to make your online apps faster without having you to think about offline/sync. For all read queries it will first look into local SQLite cache and if the data is not found, it will read the data from remote store and also update the cache. To refresh the cache you can simply purge the local table and read from it again.

This store implemenation allows you to read data even when you're offline however since it does not have sync capability, you cannot create or update items when you're offline.

Getting Started
===============

* Create a new Mobile Service in Azure
* Download the Quick Start Todo sample for Windows Store
* Install WindowsAzure.MobileService.SQLiteStore 1.0.0-alpha
* Install MobileServiceReadthruStore 1.0.0-alpha
* Replace the GetTable<TodoItem> call in MainPage with GetSyncTable<TodoItem>()
* Change the OnNavigatedTo as follows:

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.MobileService.SyncContext.IsInitialized)
            {
                var store = new MobileServiceSQLiteStore("todo.db");
                store.DefineTable<TodoItem>();
                var readthruStore = new ReadthruLocalStore(store, App.MobileService);
                await App.MobileService.SyncContext.InitializeAsync(readthruStore, new NullSyncHandler());
            }
            RefreshTodoItems();
        }
        
* Change the ButtonRefresh_Click as follows:

        private async void ButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            await todoTable.PurgeAsync();
            RefreshTodoItems();
        }
