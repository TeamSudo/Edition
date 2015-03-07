using System;
using System.Data.Services.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Net.Http;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;

// The data model defined by this file serves as a representative example of a strongly-typed
// model.  The property names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs. If using this model, you might improve app 
// responsiveness by initiating the data loading task in the code behind for App.xaml when the app 
// is first launched.

namespace Edition.Data
{
    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class NewsDataItem
    {       
        public NewsDataItem(String uniqueId, String title, String subtitle, String imagePath, String description, String date)
        {
            this.ID = uniqueId;
            this.Title = title;
            this.Url = subtitle;
            this.Description = description;
            this.Source = imagePath;
            this.Date = date;
        }

        public string ID { get; private set; }
        public string Title { get; private set; }
        public string Url { get; private set; }
        public string Description { get; private set; }
        public string Source { get; private set; }
        public string Date { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class NewsDataGroup
    {
        public NewsDataGroup(String uniqueId, String title, String subtitle, String imagePath, String description)
        {
            this.ID = uniqueId;
            this.Title = title;
            this.Url = subtitle;
            this.Description = description;
            this.Source = imagePath;
            this.Items = new ObservableCollection<NewsDataItem>();
        }

        public string ID { get; private set; }
        public string Title { get; private set; }
        public string Url { get; private set; }
        public string Description { get; private set; }
        public string Source { get; private set; }
        public ObservableCollection<NewsDataItem> Items { get; private set; }

        public override string ToString()
        {
            return this.Title;
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with date read from a static json file.
    /// 
    /// NewsDataSource initializes with data read from a static json file included in the 
    /// project.  This provides sample data at both design-time and run-time.
    /// </summary>
    public sealed class NewsDataSource
    {
        private static NewsDataSource _sampleDataSource = new NewsDataSource();
        private const string FILENAME = "ms-appx:///DataModel/SampleData.json";
        private ObservableCollection<NewsDataGroup> _groups = new ObservableCollection<NewsDataGroup>();
        public ObservableCollection<NewsDataGroup> Groups
        {
            get { return this._groups; }
        }

        public static async Task<IEnumerable<NewsDataGroup>> GetGroupsAsync()
        {
            await _sampleDataSource.GetSampleDataAsync();

            return _sampleDataSource.Groups;
        }

        public static async Task<NewsDataGroup> GetGroupAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.Where((group) => group.ID.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static async Task<NewsDataItem> GetItemAsync(string uniqueId)
        {
            await _sampleDataSource.GetSampleDataAsync();
            // Simple linear search is acceptable for small data sets
            var matches = _sampleDataSource.Groups.SelectMany(group => group.Items).Where((item) => item.ID.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        private async Task GetSampleDataAsync()
        {
            if (this._groups.Count != 0)
                return;
            
            Uri dataUri = new Uri(FILENAME);
            
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(dataUri);
            string jsonText = await FileIO.ReadTextAsync(file);
            JsonObject jsonObject = JsonObject.Parse(jsonText);
            JsonArray jsonArray = jsonObject["Groups"].GetArray();

            foreach (JsonValue groupValue in jsonArray)
            {
                JsonObject groupObject = groupValue.GetObject();
                NewsDataGroup group = new NewsDataGroup(groupObject["ID"].GetString(),
                                                            groupObject["Title"].GetString(),
                                                            groupObject["Url"].GetString(),
                                                            groupObject["Source"].GetString(),
                                                            groupObject["Description"].GetString());

                foreach (JsonValue itemValue in groupObject["Items"].GetArray())
                {
                    JsonObject itemObject = itemValue.GetObject();
                    group.Items.Add(new NewsDataItem(itemObject["ID"].GetString(),
                                                       itemObject["Title"].GetString(),
                                                       itemObject["Url"].GetString(),
                                                       itemObject["Source"].GetString(),
                                                       itemObject["Description"].GetString(),
                                                       itemObject["Date"].GetString()));
                }
                this.Groups.Add(group);
            }
        }

        static void MakeRequest()
        {

            // This is the query expression.

            string query = "Sachin Tendulkar";

            // Create a Bing container.

            string rootUrl = "https://api.datamarket.azure.com/Bing/Search";
            var AccountKey = "FBNLQ3puE3Hfj2RGL6lLQ5+GSppxLmubHCy62FScm1U=";
            var bingContainer = new Bing.BingSearchContainer(new Uri(rootUrl));

            // The market to use.

            string market = "en-in";

            // Configure bingContainer to use your credentials.

            bingContainer.Credentials = new NetworkCredential(AccountKey, AccountKey);

            // Build the query, limiting to 10 results.

            var newsQuery = bingContainer.News(query, null, market, null, null, null, null, null, null);

            newsQuery = newsQuery.AddQueryOption("$top", 10);

            // Run the query and display the results.

            var webResults = newsQuery.BeginExecute(_onNewsQueryComplete, newsQuery);
                        
        }

        async private static void _onNewsQueryComplete(IAsyncResult newsResults)
        {
            // Get the original query from the imageResults.
            DataServiceQuery<Bing.NewsResult> query =  newsResults.AsyncState as DataServiceQuery<Bing.NewsResult>;

            var resultList = new List<string>();
            var response = query.EndExecute(newsResults);

            foreach (var result in response)
                resultList.Add(result.ToString());

            await writeJsonAsync(resultList);
            
                        
        }

        private static async Task writeJsonAsync(List<String> list)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(List<string>));
                StorageFile storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                              FILENAME,
                              CreationCollisionOption.ReplaceExisting);
                var stream = await storageFile.OpenAsync(FileAccessMode.ReadWrite);
                
                using(var outputStream = stream.GetOutputStreamAt(0))
                {
                    serializer.WriteObject(outputStream as System.IO.Stream, list);
                }
            }
            catch (Exception e) {}
        }

        private static async Task readJsonAsync()
        {
            
            string content = String.Empty;

            var myStream = await ApplicationData.Current.LocalFolder.OpenStreamForReadAsync(FILENAME);
            using (StreamReader reader = new StreamReader(myStream))
            {
                content = await reader.ReadToEndAsync();
            }

        }


        private void FindNewsCompleted(NewsDataSource newsDataSource, List<string> resultList)
        {
            throw new NotImplementedException();
        }
      
                
    }
}